using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Implementation of logging service with local storage and optional central reporting
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly INetworkManager _networkManager;
    private readonly ConcurrentQueue<LogEntry> _logBuffer = new();
    private readonly string _logDirectory = "Logs";
    private readonly int _maxLogEntries = 10000;

    public LoggingService(ILogger<LoggingService> logger, INetworkManager networkManager)
    {
        _logger = logger;
        _networkManager = networkManager;
        
        // Ensure log directory exists
        Directory.CreateDirectory(_logDirectory);
    }

    public void LogInfo(string message, string component)
    {
        var entry = new LogEntry(LogLevel.Information, message, component);
        WriteLogEntry(entry);
        _logger.LogInformation("[{Component}] {Message}", component, message);
    }

    public void LogWarning(string message, string component)
    {
        var entry = new LogEntry(LogLevel.Warning, message, component);
        WriteLogEntry(entry);
        _logger.LogWarning("[{Component}] {Message}", component, message);
    }

    public void LogError(Exception ex, string context)
    {
        var entry = new LogEntry(LogLevel.Error, $"{context}: {ex.Message}", "System", ex);
        WriteLogEntry(entry);
        _logger.LogError(ex, "{Context}", context);
    }

    public void LogDebug(string message, string component)
    {
        var entry = new LogEntry(LogLevel.Debug, message, component);
        WriteLogEntry(entry);
        _logger.LogDebug("[{Component}] {Message}", component, message);
    }

    public async Task SendTelemetryAsync()
    {
        try
        {
            if (!await _networkManager.TestConnectivityAsync())
            {
                _logger.LogDebug("No connectivity available for telemetry");
                return;
            }

            // TODO: Implement telemetry sending to central server
            // This will be implemented in later tasks
            _logger.LogDebug("Telemetry sending not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send telemetry");
        }
    }

    public async Task<IEnumerable<string>> GetRecentLogsAsync(int count = 100)
    {
        try
        {
            var logFile = GetCurrentLogFile();
            if (!File.Exists(logFile))
                return Enumerable.Empty<string>();

            var lines = await File.ReadAllLinesAsync(logFile);
            return lines.TakeLast(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read recent logs");
            return Enumerable.Empty<string>();
        }
    }

    private void WriteLogEntry(LogEntry entry)
    {
        try
        {
            // Add to memory buffer
            _logBuffer.Enqueue(entry);
            
            // Trim buffer if too large
            while (_logBuffer.Count > _maxLogEntries)
            {
                _logBuffer.TryDequeue(out _);
            }

            // Write to file
            var logFile = GetCurrentLogFile();
            var logLine = FormatLogEntry(entry);
            File.AppendAllText(logFile, logLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Use system logger as fallback
            _logger.LogError(ex, "Failed to write log entry");
        }
    }

    private string GetCurrentLogFile()
    {
        var fileName = $"enterprise-agent-{DateTime.Now:yyyy-MM-dd}.log";
        return Path.Combine(_logDirectory, fileName);
    }

    private string FormatLogEntry(LogEntry entry)
    {
        var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = entry.Level.ToString().ToUpper();
        var exception = entry.Exception != null ? $" | Exception: {entry.Exception}" : "";
        
        return $"{timestamp} [{level}] [{entry.Component}] {entry.Message}{exception}";
    }

    private class LogEntry
    {
        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public string Message { get; }
        public string Component { get; }
        public Exception? Exception { get; }

        public LogEntry(LogLevel level, string message, string component, Exception? exception = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
            Component = component;
            Exception = exception;
        }
    }
}