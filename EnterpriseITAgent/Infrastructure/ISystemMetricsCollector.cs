using System;
using System.Threading.Tasks;
using EnterpriseITAgent.Models;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Interface for collecting system metrics using WMI
/// </summary>
public interface ISystemMetricsCollector
{
    /// <summary>
    /// Collects current system metrics
    /// </summary>
    /// <returns>Current system metrics</returns>
    Task<SystemMetrics> CollectMetricsAsync();

    /// <summary>
    /// Gets the current node status including metrics and heartbeat
    /// </summary>
    /// <returns>Current node status</returns>
    Task<NodeStatus> GetNodeStatusAsync();

    /// <summary>
    /// Starts the heartbeat reporting to central server
    /// </summary>
    Task StartHeartbeatAsync();

    /// <summary>
    /// Stops the heartbeat reporting
    /// </summary>
    Task StopHeartbeatAsync();

    /// <summary>
    /// Sends a single heartbeat to the central server
    /// </summary>
    Task SendHeartbeatAsync();

    /// <summary>
    /// Checks if the system is currently idle
    /// </summary>
    /// <returns>True if system is idle, false otherwise</returns>
    Task<bool> IsSystemIdleAsync();

    /// <summary>
    /// Gets available storage space for backup operations
    /// </summary>
    /// <returns>Available backup storage in GB</returns>
    Task<long> GetAvailableBackupStorageAsync();

    /// <summary>
    /// Adds a custom metric to be collected
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="valueProvider">Function to get the metric value</param>
    void AddCustomMetric(string name, Func<Task<double>> valueProvider);

    /// <summary>
    /// Removes a custom metric
    /// </summary>
    /// <param name="name">Metric name to remove</param>
    void RemoveCustomMetric(string name);

    /// <summary>
    /// Gets historical metrics for a specified time range
    /// </summary>
    /// <param name="from">Start time</param>
    /// <param name="to">End time</param>
    /// <returns>Historical metrics</returns>
    Task<SystemMetrics[]> GetHistoricalMetricsAsync(DateTime from, DateTime to);
}