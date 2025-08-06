using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using EnterpriseITAgent.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Timer = System.Timers.Timer;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Enhanced logging service with structured logging, rolling files, and telemetry
/// </summary>
public class LoggingService : ILoggingService, IDisposable
{
    private readonly ILogger<LoggingService> _logger;
    private readonly INetworkManager _networkManager;
    private readonly IConfigurationManager _configurationManager;
    private readonly ConcurrentQueue<LogEntry> _logBuffer = new();
    private readonly ConcurrentQueue<LogEntry> _telemetryBuffer = new();
    private readonly Timer _telemetryTimer;
    private readonly SemaphoreSlim _telemetrySemaphore = new(1, 1);
    
    private LoggingConfiguration _config;
    private Logger _serilogLogger;
    private string _nodeId = Environment.MachineName;
    private string? _correlationId;
    private string? _userId;
    private string? _sessionId;
    private bool _disposed;

    // Statistics tracking
    private long _totalLogs;
    private long _totalErrors;
    private long _totalWarnings;
    private readonly ConcurrentDictionary<string, int> _componentStats = new();
    private readonly ConcurrentDictionary<string, int> _errorTypeStats = new();

    public LoggingService(
        ILogger<LoggingService> logger, 
        INetworkManager networkManager,
        IConfigurationManager configurationManager)
    {
        _logger = logger;
        _networkManager = networkManager;
        _configurationManager = configurationManager;
        
        // Initialize with default configuration
        _config = new LoggingConfiguration();
        InitializeSerilog();
        
        // Set up telemetry timer
        _telemetryTimer = new Timer(TimeSpan.FromMinutes(_config.TelemetryIntervalMinutes).TotalMilliseconds);
        _telemetryTimer.Elapsed += OnTelemetryTimer;
        _telemetryTimer.AutoReset = true;
        
        if (_config.EnableTelemetry)
        {
            _telemetryTimer.Start();
        }

        // Load node ID from configuration
        LoadNodeIdAsync();
    }

    private async void LoadNodeIdAsync()
    {
        try
        {
            var config = await _configurationManager.LoadConfigurationAsync();
            if (!string.IsNullOrEmpty(config.NodeId))
            {
                _nodeId = config.NodeId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load node ID from configuration, using machine name");
        }
    }

    private void InitializeSerilog()
    {
        try
        {
            Directory.CreateDirectory(_config.LogDirectory);

            var logLevel = Enum.TryParse<LogEventLevel>(_config.MinLogLevel, out var level) 
                ? level 
                : LogEventLevel.Information;

            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    Path.Combine(_config.LogDirectory, "enterprise-agent-.json"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: _config.RetainedFileCount,
                    fileSizeLimitBytes: _config.MaxFileSizeMB * 1024 * 1024,
                    rollOnFileSizeLimit: true)
                .WriteTo.File(
                    Path.Combine(_config.LogDirectory, "enterprise-agent-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: _config.RetainedFileCount,
                    fileSizeLimitBytes: _config.MaxFileSizeMB * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Component}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Serilog");
        }
    }

    public void LogInfo(string message, string component, Dictionary<string, object>? properties = null)
    {
        var entry = CreateLogEntry(LogLevel.Information, message, component, null, properties);
        ProcessLogEntry(entry);
    }

    public void LogWarning(string message, string component, Dictionary<string, object>? properties = null)
    {
        var entry = CreateLogEntry(LogLevel.Warning, message, component, null, properties);
        ProcessLogEntry(entry);
        Interlocked.Increment(ref _totalWarnings);
    }

    public void LogError(Exception ex, string context, string component = "System", Dictionary<string, object>? properties = null)
    {
        var entry = CreateLogEntry(LogLevel.Error, $"{context}: {ex.Message}", component, ex, properties);
        ProcessLogEntry(entry);
        Interlocked.Increment(ref _totalErrors);
        
        // Track error types
        var errorType = ex.GetType().Name;
        _errorTypeStats.AddOrUpdate(errorType, 1, (key, value) => value + 1);
    }

    public void LogDebug(string message, string component, Dictionary<string, object>? properties = null)
    {
        var entry = CreateLogEntry(LogLevel.Debug, message, component, null, properties);
        ProcessLogEntry(entry);
    }

    public void LogCritical(string message, string component, Dictionary<string, object>? properties = null)
    {
        var entry = CreateLogEntry(LogLevel.Critical, message, component, null, properties);
        ProcessLogEntry(entry);
    }

    public void LogStructured(LogEntry logEntry)
    {
        ProcessLogEntry(logEntry);
    }

    public async Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100)
    {
        try
        {
            var logs = new List<LogEntry>();
            
            // Get from memory buffer first
            var bufferLogs = _logBuffer.ToArray().TakeLast(count / 2);
            logs.AddRange(bufferLogs);
            
            // Get remaining from file if needed
            var remaining = count - logs.Count;
            if (remaining > 0)
            {
                var fileLogs = await ReadLogsFromFileAsync(remaining);
                logs.AddRange(fileLogs);
            }
            
            return logs.OrderByDescending(l => l.Timestamp).Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent logs");
            return Enumerable.Empty<LogEntry>();
        }
    }

    public async Task<IEnumerable<LogEntry>> GetLogsByLevelAsync(LogLevel level, int count = 100)
    {
        var allLogs = await GetRecentLogsAsync(count * 2); // Get more to filter
        return allLogs.Where(l => l.Level == level).Take(count);
    }

    public async Task<IEnumerable<LogEntry>> GetLogsByComponentAsync(string component, int count = 100)
    {
        var allLogs = await GetRecentLogsAsync(count * 2); // Get more to filter
        return allLogs.Where(l => l.Component.Equals(component, StringComparison.OrdinalIgnoreCase)).Take(count);
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationId = correlationId;
    }

    public void SetUserContext(string userId, string? sessionId = null)
    {
        _userId = userId;
        _sessionId = sessionId;
    }

    public async Task ConfigureLoggingAsync(LoggingConfiguration config)
    {
        _config = config;
        
        // Reinitialize Serilog with new configuration
        _serilogLogger?.Dispose();
        InitializeSerilog();
        
        // Update telemetry timer
        _telemetryTimer.Stop();
        _telemetryTimer.Interval = TimeSpan.FromMinutes(_config.TelemetryIntervalMinutes).TotalMilliseconds;
        
        if (_config.EnableTelemetry)
        {
            _telemetryTimer.Start();
        }
        
        LogInfo("Logging configuration updated", "LoggingService");
    }

    public async Task<Dictionary<string, object>> GetLoggingStatsAsync()
    {
        return new Dictionary<string, object>
        {
            ["TotalLogs"] = _totalLogs,
            ["TotalErrors"] = _totalErrors,
            ["TotalWarnings"] = _totalWarnings,
            ["BufferSize"] = _logBuffer.Count,
            ["TelemetryBufferSize"] = _telemetryBuffer.Count,
            ["ComponentStats"] = _componentStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ["ErrorTypeStats"] = _errorTypeStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ["NodeId"] = _nodeId,
            ["LogDirectory"] = _config.LogDirectory,
            ["TelemetryEnabled"] = _config.EnableTelemetry
        };
    }

    public async Task SendTelemetryAsync()
    {
        if (!_config.EnableTelemetry || string.IsNullOrEmpty(_config.TelemetryEndpoint))
        {
            return;
        }

        await _telemetrySemaphore.WaitAsync();
        try
        {
            if (!await _networkManager.TestConnectivityAsync())
            {
                LogDebug("No connectivity available for telemetry", "LoggingService");
                return;
            }

            var telemetryData = await CreateTelemetryDataAsync();
            if (telemetryData.LogEntries.Count == 0)
            {
                return; // Nothing to send
            }

            await _networkManager.SecureApiCallAsync<object>(_config.TelemetryEndpoint, telemetryData);
            
            // Clear sent entries from telemetry buffer
            var sentCount = telemetryData.LogEntries.Count;
            for (int i = 0; i < sentCount; i++)
            {
                _telemetryBuffer.TryDequeue(out _);
            }
            
            LogDebug($"Sent {sentCount} log entries via telemetry", "LoggingService");
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to send telemetry", "LoggingService");
        }
        finally
        {
            _telemetrySemaphore.Release();
        }
    }

    private LogEntry CreateLogEntry(LogLevel level, string message, string component, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        var entry = new LogEntry
        {
            Level = level,
            Message = message,
            Component = component,
            NodeId = _nodeId,
            CorrelationId = _correlationId,
            UserId = _userId,
            SessionId = _sessionId,
            Properties = properties
        };

        if (exception != null)
        {
            entry.Exception = ExceptionDetails.FromException(exception);
        }

        return entry;
    }

    private void ProcessLogEntry(LogEntry entry)
    {
        try
        {
            // Add to memory buffer
            _logBuffer.Enqueue(entry);
            Interlocked.Increment(ref _totalLogs);
            
            // Track component statistics
            _componentStats.AddOrUpdate(entry.Component, 1, (key, value) => value + 1);
            
            // Trim buffer if too large
            while (_logBuffer.Count > _config.MaxBufferSize)
            {
                _logBuffer.TryDequeue(out _);
            }

            // Add to telemetry buffer for errors and warnings
            if (entry.Level >= LogLevel.Warning)
            {
                _telemetryBuffer.Enqueue(entry);
                
                // Trim telemetry buffer
                while (_telemetryBuffer.Count > _config.MaxBufferSize / 2)
                {
                    _telemetryBuffer.TryDequeue(out _);
                }
            }

            // Write to Serilog
            WriteSerilogEntry(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process log entry");
        }
    }

    private void WriteSerilogEntry(LogEntry entry)
    {
        try
        {
            var template = "[{Component}] {Message}";
            var properties = new List<object> { entry.Component, entry.Message };

            if (entry.Properties != null)
            {
                foreach (var prop in entry.Properties)
                {
                    template += $" {{{prop.Key}}}";
                    properties.Add(prop.Value);
                }
            }

            var logEvent = entry.Level switch
            {
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };

            _serilogLogger.Write(logEvent, entry.Exception?.Message, template, properties.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write Serilog entry");
        }
    }

    private async Task<List<LogEntry>> ReadLogsFromFileAsync(int count)
    {
        var logs = new List<LogEntry>();
        
        try
        {
            var jsonLogFile = Path.Combine(_config.LogDirectory, $"enterprise-agent-{DateTime.Now:yyyyMMdd}.json");
            if (File.Exists(jsonLogFile))
            {
                var lines = await File.ReadAllLinesAsync(jsonLogFile);
                foreach (var line in lines.TakeLast(count))
                {
                    try
                    {
                        var logEntry = JsonSerializer.Deserialize<LogEntry>(line);
                        if (logEntry != null)
                        {
                            logs.Add(logEntry);
                        }
                    }
                    catch
                    {
                        // Skip malformed entries
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read logs from file");
        }
        
        return logs;
    }

    private async Task<TelemetryData> CreateTelemetryDataAsync()
    {
        var telemetryData = new TelemetryData
        {
            NodeId = _nodeId,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
            LogEntries = _telemetryBuffer.ToList(),
            ErrorSummary = new ErrorSummary
            {
                TotalErrors = (int)_totalErrors,
                TotalWarnings = (int)_totalWarnings,
                CommonErrors = _errorTypeStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CriticalErrors = _telemetryBuffer
                    .Where(l => l.Level == LogLevel.Critical)
                    .Select(l => l.Message)
                    .ToList()
            },
            PerformanceCounters = new Dictionary<string, double>
            {
                ["LogsPerMinute"] = _totalLogs / Math.Max(1, DateTime.UtcNow.Subtract(Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMinutes),
                ["ErrorRate"] = _totalLogs > 0 ? (double)_totalErrors / _totalLogs : 0,
                ["WarningRate"] = _totalLogs > 0 ? (double)_totalWarnings / _totalLogs : 0
            }
        };

        return telemetryData;
    }

    private async void OnTelemetryTimer(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await SendTelemetryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in telemetry timer");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _telemetryTimer?.Stop();
        _telemetryTimer?.Dispose();
        _serilogLogger?.Dispose();
        _telemetrySemaphore?.Dispose();
        
        _disposed = true;
    }
}