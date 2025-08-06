using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnterpriseITAgent.Models;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Interface for comprehensive logging with local storage and optional central reporting
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Logs an informational message
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="component">The component generating the log</param>
    /// <param name="properties">Additional structured properties</param>
    void LogInfo(string message, string component, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="component">The component generating the warning</param>
    /// <param name="properties">Additional structured properties</param>
    void LogWarning(string message, string component, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs an error with exception details
    /// </summary>
    /// <param name="ex">The exception to log</param>
    /// <param name="context">Additional context about the error</param>
    /// <param name="component">The component generating the error</param>
    /// <param name="properties">Additional structured properties</param>
    void LogError(Exception ex, string context, string component = "System", Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="message">The debug message</param>
    /// <param name="component">The component generating the debug message</param>
    /// <param name="properties">Additional structured properties</param>
    void LogDebug(string message, string component, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a critical error
    /// </summary>
    /// <param name="message">The critical error message</param>
    /// <param name="component">The component generating the critical error</param>
    /// <param name="properties">Additional structured properties</param>
    void LogCritical(string message, string component, Dictionary<string, object>? properties = null);

    /// <summary>
    /// Logs a structured log entry
    /// </summary>
    /// <param name="logEntry">The structured log entry</param>
    void LogStructured(LogEntry logEntry);

    /// <summary>
    /// Sends telemetry data to central server
    /// </summary>
    Task SendTelemetryAsync();

    /// <summary>
    /// Gets recent log entries for display
    /// </summary>
    /// <param name="count">Number of recent entries to retrieve</param>
    /// <returns>List of recent log entries</returns>
    Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100);

    /// <summary>
    /// Gets log entries by level
    /// </summary>
    /// <param name="level">Log level to filter by</param>
    /// <param name="count">Number of entries to retrieve</param>
    /// <returns>List of log entries</returns>
    Task<IEnumerable<LogEntry>> GetLogsByLevelAsync(LogLevel level, int count = 100);

    /// <summary>
    /// Gets log entries by component
    /// </summary>
    /// <param name="component">Component to filter by</param>
    /// <param name="count">Number of entries to retrieve</param>
    /// <returns>List of log entries</returns>
    Task<IEnumerable<LogEntry>> GetLogsByComponentAsync(string component, int count = 100);

    /// <summary>
    /// Sets the correlation ID for tracking related operations
    /// </summary>
    /// <param name="correlationId">Correlation ID</param>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Sets the user context for logging
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="sessionId">Session ID</param>
    void SetUserContext(string userId, string? sessionId = null);

    /// <summary>
    /// Configures logging settings
    /// </summary>
    /// <param name="config">Logging configuration</param>
    Task ConfigureLoggingAsync(LoggingConfiguration config);

    /// <summary>
    /// Gets current logging statistics
    /// </summary>
    /// <returns>Logging statistics</returns>
    Task<Dictionary<string, object>> GetLoggingStatsAsync();
}