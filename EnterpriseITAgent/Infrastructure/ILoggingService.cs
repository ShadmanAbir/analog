using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    void LogInfo(string message, string component);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="message">The warning message</param>
    /// <param name="component">The component generating the warning</param>
    void LogWarning(string message, string component);

    /// <summary>
    /// Logs an error with exception details
    /// </summary>
    /// <param name="ex">The exception to log</param>
    /// <param name="context">Additional context about the error</param>
    void LogError(Exception ex, string context);

    /// <summary>
    /// Logs a debug message (only in debug builds)
    /// </summary>
    /// <param name="message">The debug message</param>
    /// <param name="component">The component generating the debug message</param>
    void LogDebug(string message, string component);

    /// <summary>
    /// Sends telemetry data to central server
    /// </summary>
    Task SendTelemetryAsync();

    /// <summary>
    /// Gets recent log entries for display
    /// </summary>
    /// <param name="count">Number of recent entries to retrieve</param>
    /// <returns>List of recent log entries</returns>
    Task<IEnumerable<string>> GetRecentLogsAsync(int count = 100);
}