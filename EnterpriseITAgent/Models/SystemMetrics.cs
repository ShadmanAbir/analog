using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EnterpriseITAgent.Models;

/// <summary>
/// System metrics for monitoring and telemetry
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// CPU usage percentage (0-100)
    /// </summary>
    [JsonPropertyName("cpuUsage")]
    public double CpuUsage { get; set; }

    /// <summary>
    /// Memory usage percentage (0-100)
    /// </summary>
    [JsonPropertyName("memoryUsage")]
    public double MemoryUsage { get; set; }

    /// <summary>
    /// Total memory in MB
    /// </summary>
    [JsonPropertyName("totalMemoryMB")]
    public long TotalMemoryMB { get; set; }

    /// <summary>
    /// Available memory in MB
    /// </summary>
    [JsonPropertyName("availableMemoryMB")]
    public long AvailableMemoryMB { get; set; }

    /// <summary>
    /// Disk usage percentage (0-100)
    /// </summary>
    [JsonPropertyName("diskUsage")]
    public double DiskUsage { get; set; }

    /// <summary>
    /// Total disk space in GB
    /// </summary>
    [JsonPropertyName("totalDiskGB")]
    public long TotalDiskGB { get; set; }

    /// <summary>
    /// Available disk space in GB
    /// </summary>
    [JsonPropertyName("availableDiskGB")]
    public long AvailableDiskGB { get; set; }

    /// <summary>
    /// Network bytes received since last measurement
    /// </summary>
    [JsonPropertyName("networkBytesIn")]
    public long NetworkBytesIn { get; set; }

    /// <summary>
    /// Network bytes sent since last measurement
    /// </summary>
    [JsonPropertyName("networkBytesOut")]
    public long NetworkBytesOut { get; set; }

    /// <summary>
    /// Number of running processes
    /// </summary>
    [JsonPropertyName("processCount")]
    public int ProcessCount { get; set; }

    /// <summary>
    /// System uptime in hours
    /// </summary>
    [JsonPropertyName("uptimeHours")]
    public double UptimeHours { get; set; }

    /// <summary>
    /// Timestamp when metrics were collected
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional custom metrics
    /// </summary>
    [JsonPropertyName("customMetrics")]
    public Dictionary<string, double>? CustomMetrics { get; set; }
}

/// <summary>
/// Node status information including heartbeat
/// </summary>
public class NodeStatus
{
    /// <summary>
    /// Unique node identifier
    /// </summary>
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Last heartbeat timestamp
    /// </summary>
    [JsonPropertyName("lastHeartbeat")]
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current system metrics
    /// </summary>
    [JsonPropertyName("metrics")]
    public SystemMetrics Metrics { get; set; } = new();

    /// <summary>
    /// List of active services
    /// </summary>
    [JsonPropertyName("activeServices")]
    public List<string> ActiveServices { get; set; } = new();

    /// <summary>
    /// Current system alerts
    /// </summary>
    [JsonPropertyName("alerts")]
    public List<SystemAlert> Alerts { get; set; } = new();

    /// <summary>
    /// Application version
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Operating system information
    /// </summary>
    [JsonPropertyName("osInfo")]
    public string OsInfo { get; set; } = string.Empty;

    /// <summary>
    /// Machine name
    /// </summary>
    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = Environment.MachineName;

    /// <summary>
    /// Current user
    /// </summary>
    [JsonPropertyName("currentUser")]
    public string CurrentUser { get; set; } = Environment.UserName;

    /// <summary>
    /// Whether the node is currently idle
    /// </summary>
    [JsonPropertyName("isIdle")]
    public bool IsIdle { get; set; }

    /// <summary>
    /// Available storage for backup operations in GB
    /// </summary>
    [JsonPropertyName("availableBackupStorageGB")]
    public long AvailableBackupStorageGB { get; set; }
}

/// <summary>
/// System alert information
/// </summary>
public class SystemAlert
{
    /// <summary>
    /// Alert identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Alert severity level
    /// </summary>
    [JsonPropertyName("severity")]
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Alert message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Component that generated the alert
    /// </summary>
    [JsonPropertyName("component")]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when alert was created
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the alert has been acknowledged
    /// </summary>
    [JsonPropertyName("acknowledged")]
    public bool Acknowledged { get; set; }

    /// <summary>
    /// Additional alert data
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Informational alert
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning alert
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error alert
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical alert requiring immediate attention
    /// </summary>
    Critical = 3
}