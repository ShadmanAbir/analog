using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EnterpriseITAgent.Models;

/// <summary>
/// Telemetry data package for central reporting
/// </summary>
public class TelemetryData
{
    /// <summary>
    /// Node ID sending the telemetry
    /// </summary>
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when telemetry was collected
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Application version
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Log entries to send
    /// </summary>
    [JsonPropertyName("logEntries")]
    public List<LogEntry> LogEntries { get; set; } = new();

    /// <summary>
    /// System metrics
    /// </summary>
    [JsonPropertyName("systemMetrics")]
    public SystemMetrics? SystemMetrics { get; set; }

    /// <summary>
    /// Performance counters
    /// </summary>
    [JsonPropertyName("performanceCounters")]
    public Dictionary<string, double>? PerformanceCounters { get; set; }

    /// <summary>
    /// Error summary statistics
    /// </summary>
    [JsonPropertyName("errorSummary")]
    public ErrorSummary? ErrorSummary { get; set; }

    /// <summary>
    /// Feature usage statistics
    /// </summary>
    [JsonPropertyName("featureUsage")]
    public Dictionary<string, int>? FeatureUsage { get; set; }
}

/// <summary>
/// Error summary for telemetry reporting
/// </summary>
public class ErrorSummary
{
    /// <summary>
    /// Total number of errors in this period
    /// </summary>
    [JsonPropertyName("totalErrors")]
    public int TotalErrors { get; set; }

    /// <summary>
    /// Total number of warnings in this period
    /// </summary>
    [JsonPropertyName("totalWarnings")]
    public int TotalWarnings { get; set; }

    /// <summary>
    /// Most common error types
    /// </summary>
    [JsonPropertyName("commonErrors")]
    public Dictionary<string, int> CommonErrors { get; set; } = new();

    /// <summary>
    /// Critical errors that need immediate attention
    /// </summary>
    [JsonPropertyName("criticalErrors")]
    public List<string> CriticalErrors { get; set; } = new();
}

/// <summary>
/// Configuration for logging behavior
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Minimum log level to write to files
    /// </summary>
    [JsonPropertyName("minLogLevel")]
    public string MinLogLevel { get; set; } = "Information";

    /// <summary>
    /// Maximum size of each log file in MB
    /// </summary>
    [JsonPropertyName("maxFileSizeMB")]
    public int MaxFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Number of log files to retain
    /// </summary>
    [JsonPropertyName("retainedFileCount")]
    public int RetainedFileCount { get; set; } = 7;

    /// <summary>
    /// Directory where log files are stored
    /// </summary>
    [JsonPropertyName("logDirectory")]
    public string LogDirectory { get; set; } = "Logs";

    /// <summary>
    /// Whether to enable telemetry reporting
    /// </summary>
    [JsonPropertyName("enableTelemetry")]
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Interval in minutes for telemetry reporting
    /// </summary>
    [JsonPropertyName("telemetryIntervalMinutes")]
    public int TelemetryIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Central API endpoint for telemetry
    /// </summary>
    [JsonPropertyName("telemetryEndpoint")]
    public string TelemetryEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Whether to include sensitive data in logs
    /// </summary>
    [JsonPropertyName("includeSensitiveData")]
    public bool IncludeSensitiveData { get; set; } = false;

    /// <summary>
    /// Maximum number of log entries to buffer in memory
    /// </summary>
    [JsonPropertyName("maxBufferSize")]
    public int MaxBufferSize { get; set; } = 1000;
}