using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseITAgent.Models;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// System metrics collector using WMI for CPU, RAM, disk usage
/// </summary>
public class SystemMetricsCollector : ISystemMetricsCollector, IDisposable
{
    private readonly ILogger<SystemMetricsCollector> _logger;
    private readonly INetworkManager _networkManager;
    private readonly IConfigurationManager _configurationManager;
    private readonly Dictionary<string, Func<Task<double>>> _customMetrics;
    private readonly Timer _heartbeatTimer;
    private readonly SemaphoreSlim _metricsLock;
    private bool _disposed;
    private string _nodeId = string.Empty;
    private long _previousNetworkBytesReceived;
    private long _previousNetworkBytesSent;
    private DateTime _previousNetworkMeasurement;

    public SystemMetricsCollector(
        ILogger<SystemMetricsCollector> logger,
        INetworkManager networkManager,
        IConfigurationManager configurationManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
        _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        _customMetrics = new Dictionary<string, Func<Task<double>>>();
        _metricsLock = new SemaphoreSlim(1, 1);
        _previousNetworkMeasurement = DateTime.UtcNow;

        // Initialize heartbeat timer (5 minutes interval)
        _heartbeatTimer = new Timer(HeartbeatCallback, null, Timeout.Infinite, Timeout.Infinite);
        
        InitializeNodeId();
    }

    /// <summary>
    /// Collects current system metrics using WMI
    /// </summary>
    public async Task<SystemMetrics> CollectMetricsAsync()
    {
        await _metricsLock.WaitAsync();
        try
        {
            var metrics = new SystemMetrics
            {
                Timestamp = DateTime.UtcNow
            };

            // Collect metrics in parallel for better performance
            var tasks = new[]
            {
                CollectCpuUsageAsync().ContinueWith(t => metrics.CpuUsage = t.Result),
                CollectMemoryUsageAsync().ContinueWith(t => 
                {
                    metrics.MemoryUsage = t.Result.Usage;
                    metrics.TotalMemoryMB = t.Result.Total;
                    metrics.AvailableMemoryMB = t.Result.Available;
                }),
                CollectDiskUsageAsync().ContinueWith(t => 
                {
                    metrics.DiskUsage = t.Result.Usage;
                    metrics.TotalDiskGB = t.Result.Total;
                    metrics.AvailableDiskGB = t.Result.Available;
                }),
                CollectNetworkUsageAsync().ContinueWith(t => 
                {
                    metrics.NetworkBytesIn = t.Result.BytesIn;
                    metrics.NetworkBytesOut = t.Result.BytesOut;
                }),
                CollectProcessCountAsync().ContinueWith(t => metrics.ProcessCount = t.Result),
                CollectUptimeAsync().ContinueWith(t => metrics.UptimeHours = t.Result)
            };

            await Task.WhenAll(tasks);

            // Collect custom metrics
            if (_customMetrics.Count > 0)
            {
                metrics.CustomMetrics = new Dictionary<string, double>();
                foreach (var customMetric in _customMetrics)
                {
                    try
                    {
                        var value = await customMetric.Value();
                        metrics.CustomMetrics[customMetric.Key] = value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to collect custom metric {MetricName}", customMetric.Key);
                    }
                }
            }

            _logger.LogDebug("System metrics collected successfully");
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics");
            throw;
        }
        finally
        {
            _metricsLock.Release();
        }
    }

    /// <summary>
    /// Gets the current node status including metrics and heartbeat
    /// </summary>
    public async Task<NodeStatus> GetNodeStatusAsync()
    {
        try
        {
            var metrics = await CollectMetricsAsync();
            var activeServices = await GetActiveServicesAsync();
            var alerts = await GetSystemAlertsAsync();
            var isIdle = await IsSystemIdleAsync();
            var availableBackupStorage = await GetAvailableBackupStorageAsync();

            var nodeStatus = new NodeStatus
            {
                NodeId = _nodeId,
                LastHeartbeat = DateTime.UtcNow,
                Metrics = metrics,
                ActiveServices = activeServices,
                Alerts = alerts,
                Version = GetApplicationVersion(),
                OsInfo = GetOperatingSystemInfo(),
                MachineName = Environment.MachineName,
                CurrentUser = Environment.UserName,
                IsIdle = isIdle,
                AvailableBackupStorageGB = availableBackupStorage
            };

            return nodeStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get node status");
            throw;
        }
    }

    /// <summary>
    /// Starts the heartbeat reporting to central server (5-minute interval)
    /// </summary>
    public async Task StartHeartbeatAsync()
    {
        try
        {
            _logger.LogInformation("Starting heartbeat reporting with 5-minute interval");
            
            // Send initial heartbeat
            await SendHeartbeatAsync();
            
            // Start timer for 5-minute intervals
            _heartbeatTimer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start heartbeat reporting");
            throw;
        }
    }

    /// <summary>
    /// Stops the heartbeat reporting
    /// </summary>
    public async Task StopHeartbeatAsync()
    {
        try
        {
            _logger.LogInformation("Stopping heartbeat reporting");
            _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop heartbeat reporting");
            throw;
        }
    }

    /// <summary>
    /// Sends a single heartbeat to the central server
    /// </summary>
    public async Task SendHeartbeatAsync()
    {
        try
        {
            var nodeStatus = await GetNodeStatusAsync();
            
            // Send heartbeat to central server via NetworkManager
            await _networkManager.SecureApiCallAsync<object>("api/heartbeat", nodeStatus);
            
            _logger.LogDebug("Heartbeat sent successfully for node {NodeId}", _nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat for node {NodeId}", _nodeId);
            // Don't throw - heartbeat failures shouldn't crash the application
        }
    }

    /// <summary>
    /// Checks if the system is currently idle based on CPU usage and user activity
    /// </summary>
    public async Task<bool> IsSystemIdleAsync()
    {
        try
        {
            // Check CPU usage over the last minute
            var cpuUsage = await CollectCpuUsageAsync();
            
            // Check user idle time using Win32 API
            var userIdleTime = GetUserIdleTime();
            
            // System is considered idle if:
            // - CPU usage is below 20%
            // - User has been idle for more than 10 minutes
            var isIdle = cpuUsage < 20.0 && userIdleTime > TimeSpan.FromMinutes(10);
            
            return isIdle;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check system idle status");
            return false;
        }
    }

    /// <summary>
    /// Gets available storage space for backup operations
    /// </summary>
    public async Task<long> GetAvailableBackupStorageAsync()
    {
        try
        {
            var config = await _configurationManager.LoadConfigurationAsync();
            
            // Use the first backup path if available, otherwise use temp path
            var backupPath = config.Backup?.BackupPaths?.FirstOrDefault() ?? Path.GetTempPath();
            
            var driveInfo = new DriveInfo(Path.GetPathRoot(backupPath) ?? "C:");
            var availableGB = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
            
            // Reserve 10% of available space for system operations
            var reservedSpace = (long)(availableGB * 0.1);
            var availableForBackup = Math.Max(0, availableGB - reservedSpace);
            
            return availableForBackup;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get available backup storage");
            return 0;
        }
    }

    /// <summary>
    /// Adds a custom metric to be collected
    /// </summary>
    public void AddCustomMetric(string name, Func<Task<double>> valueProvider)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
        
        if (valueProvider == null)
            throw new ArgumentNullException(nameof(valueProvider));

        _customMetrics[name] = valueProvider;
        _logger.LogDebug("Added custom metric: {MetricName}", name);
    }

    /// <summary>
    /// Removes a custom metric
    /// </summary>
    public void RemoveCustomMetric(string name)
    {
        if (_customMetrics.Remove(name))
        {
            _logger.LogDebug("Removed custom metric: {MetricName}", name);
        }
    }

    /// <summary>
    /// Gets historical metrics for a specified time range
    /// </summary>
    public async Task<SystemMetrics[]> GetHistoricalMetricsAsync(DateTime from, DateTime to)
    {
        // This would typically query a local database or cache
        // For now, return empty array as historical storage is not implemented
        await Task.CompletedTask;
        return Array.Empty<SystemMetrics>();
    }

    #region Private Methods

    private void InitializeNodeId()
    {
        try
        {
            // Generate node ID from MAC address + username as specified in requirements
            var macAddress = GetMacAddress();
            var username = Environment.UserName;
            _nodeId = $"{macAddress}_{username}".Replace(":", "").Replace("-", "");
            
            _logger.LogInformation("Node ID initialized: {NodeId}", _nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize node ID");
            _nodeId = Environment.MachineName + "_" + Environment.UserName;
        }
    }

    private async Task<double> CollectCpuUsageAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            var cpuUsages = new List<double>();
            
            await Task.Run(() =>
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var usage = Convert.ToDouble(obj["LoadPercentage"]);
                    cpuUsages.Add(usage);
                }
            });

            return cpuUsages.Count > 0 ? cpuUsages.Average() : 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect CPU usage via WMI");
            return 0.0;
        }
    }

    private async Task<(double Usage, long Total, long Available)> CollectMemoryUsageAsync()
    {
        try
        {
            long totalMemory = 0;
            long availableMemory = 0;

            await Task.Run(() =>
            {
                // Get total physical memory
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        totalMemory = Convert.ToInt64(obj["TotalPhysicalMemory"]) / (1024 * 1024); // Convert to MB
                        break;
                    }
                }

                // Get available memory
                using (var searcher = new ManagementObjectSearcher("SELECT AvailableBytes FROM Win32_PerfRawData_PerfOS_Memory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        availableMemory = Convert.ToInt64(obj["AvailableBytes"]) / (1024 * 1024); // Convert to MB
                        break;
                    }
                }
            });

            var usedMemory = totalMemory - availableMemory;
            var usage = totalMemory > 0 ? (double)usedMemory / totalMemory * 100.0 : 0.0;

            return (usage, totalMemory, availableMemory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect memory usage via WMI");
            return (0.0, 0, 0);
        }
    }

    private async Task<(double Usage, long Total, long Available)> CollectDiskUsageAsync()
    {
        try
        {
            long totalSpace = 0;
            long freeSpace = 0;

            await Task.Run(() =>
            {
                // Get disk usage for C: drive (system drive)
                using var searcher = new ManagementObjectSearcher("SELECT Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3 AND DeviceID='C:'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    totalSpace = Convert.ToInt64(obj["Size"]) / (1024 * 1024 * 1024); // Convert to GB
                    freeSpace = Convert.ToInt64(obj["FreeSpace"]) / (1024 * 1024 * 1024); // Convert to GB
                    break;
                }
            });

            var usedSpace = totalSpace - freeSpace;
            var usage = totalSpace > 0 ? (double)usedSpace / totalSpace * 100.0 : 0.0;

            return (usage, totalSpace, freeSpace);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect disk usage via WMI");
            return (0.0, 0, 0);
        }
    }

    private async Task<(long BytesIn, long BytesOut)> CollectNetworkUsageAsync()
    {
        try
        {
            long currentBytesReceived = 0;
            long currentBytesSent = 0;
            var currentTime = DateTime.UtcNow;

            await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher("SELECT BytesReceivedPerSec, BytesSentPerSec FROM Win32_PerfRawData_Tcpip_NetworkInterface WHERE Name != 'Loopback Pseudo-Interface 1'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    currentBytesReceived += Convert.ToInt64(obj["BytesReceivedPerSec"]);
                    currentBytesSent += Convert.ToInt64(obj["BytesSentPerSec"]);
                }
            });

            // Calculate delta since last measurement
            var deltaReceived = currentBytesReceived - _previousNetworkBytesReceived;
            var deltaSent = currentBytesSent - _previousNetworkBytesSent;
            var timeDelta = (currentTime - _previousNetworkMeasurement).TotalSeconds;

            // Update previous values
            _previousNetworkBytesReceived = currentBytesReceived;
            _previousNetworkBytesSent = currentBytesSent;
            _previousNetworkMeasurement = currentTime;

            // Return bytes per second if we have a valid time delta
            if (timeDelta > 0)
            {
                return ((long)(deltaReceived / timeDelta), (long)(deltaSent / timeDelta));
            }

            return (0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect network usage via WMI");
            return (0, 0);
        }
    }

    private async Task<int> CollectProcessCountAsync()
    {
        try
        {
            int processCount = 0;

            await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Process");
                processCount = searcher.Get().Count;
            });

            return processCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect process count via WMI");
            return Process.GetProcesses().Length; // Fallback to .NET method
        }
    }

    private async Task<double> CollectUptimeAsync()
    {
        try
        {
            DateTime bootTime = DateTime.MinValue;

            await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var bootTimeString = obj["LastBootUpTime"].ToString();
                    if (bootTimeString != null)
                    {
                        bootTime = ManagementDateTimeConverter.ToDateTime(bootTimeString);
                    }
                    break;
                }
            });

            if (bootTime != DateTime.MinValue)
            {
                return (DateTime.Now - bootTime).TotalHours;
            }

            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect uptime via WMI");
            return Environment.TickCount64 / (1000.0 * 60.0 * 60.0); // Fallback to tick count
        }
    }

    private async Task<List<string>> GetActiveServicesAsync()
    {
        var services = new List<string>();
        
        try
        {
            await Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, State FROM Win32_Service WHERE State='Running'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var serviceName = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        services.Add(serviceName);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active services");
        }

        return services;
    }

    private async Task<List<SystemAlert>> GetSystemAlertsAsync()
    {
        var alerts = new List<SystemAlert>();
        
        try
        {
            var metrics = await CollectMetricsAsync();
            
            // Generate alerts based on system metrics
            if (metrics.CpuUsage > 90)
            {
                alerts.Add(new SystemAlert
                {
                    Severity = AlertSeverity.Warning,
                    Message = $"High CPU usage: {metrics.CpuUsage:F1}%",
                    Component = "SystemMetrics"
                });
            }

            if (metrics.MemoryUsage > 90)
            {
                alerts.Add(new SystemAlert
                {
                    Severity = AlertSeverity.Warning,
                    Message = $"High memory usage: {metrics.MemoryUsage:F1}%",
                    Component = "SystemMetrics"
                });
            }

            if (metrics.DiskUsage > 90)
            {
                alerts.Add(new SystemAlert
                {
                    Severity = AlertSeverity.Critical,
                    Message = $"Low disk space: {metrics.DiskUsage:F1}% used",
                    Component = "SystemMetrics"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate system alerts");
        }

        return alerts;
    }

    private string GetMacAddress()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapter WHERE NetConnectionStatus=2");
            foreach (ManagementObject obj in searcher.Get())
            {
                var macAddress = obj["MACAddress"]?.ToString();
                if (!string.IsNullOrEmpty(macAddress))
                {
                    return macAddress;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get MAC address via WMI");
        }

        return "UNKNOWN_MAC";
    }

    private string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string GetOperatingSystemInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var caption = obj["Caption"]?.ToString();
                var version = obj["Version"]?.ToString();
                return $"{caption} ({version})";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get OS info via WMI");
        }

        return Environment.OSVersion.ToString();
    }

    private TimeSpan GetUserIdleTime()
    {
        try
        {
            // This would require P/Invoke to GetLastInputInfo Win32 API
            // For now, return a default value
            return TimeSpan.FromMinutes(5);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private async void HeartbeatCallback(object? state)
    {
        await SendHeartbeatAsync();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _heartbeatTimer?.Dispose();
            _metricsLock?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}