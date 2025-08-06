using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Models;

/// <summary>
/// Represents a structured log entry with telemetry capabilities
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Unique identifier for this log entry
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the log entry was created
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Log level (Debug, Info, Warning, Error, Critical)
    /// </summary>
    [JsonPropertyName("level")]
    public LogLevel Level { get; set; }

    /// <summary>
    /// The log message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Component or service that generated the log
    /// </summary>
    [JsonPropertyName("component")]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Node ID where the log was generated
    /// </summary>
    [JsonPropertyName("nodeId")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Exception details if this is an error log
    /// </summary>
    [JsonPropertyName("exception")]
    public ExceptionDetails? Exception { get; set; }

    /// <summary>
    /// Additional structured properties
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// User context if available
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Session ID if available
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
}

/// <summary>
/// Structured exception details for logging
/// </summary>
public class ExceptionDetails
{
    /// <summary>
    /// Exception type name
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Exception message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Stack trace
    /// </summary>
    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    /// <summary>
    /// Inner exception details
    /// </summary>
    [JsonPropertyName("innerException")]
    public ExceptionDetails? InnerException { get; set; }

    /// <summary>
    /// Additional exception data
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Creates ExceptionDetails from a .NET Exception
    /// </summary>
    public static ExceptionDetails FromException(Exception ex)
    {
        var details = new ExceptionDetails
        {
            Type = ex.GetType().FullName ?? ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace
        };

        if (ex.InnerException != null)
        {
            details.InnerException = FromException(ex.InnerException);
        }

        if (ex.Data.Count > 0)
        {
            details.Data = new Dictionary<string, object>();
            foreach (var key in ex.Data.Keys)
            {
                if (key != null)
                {
                    details.Data[key.ToString() ?? "unknown"] = ex.Data[key] ?? "null";
                }
            }
        }

        return details;
    }
}