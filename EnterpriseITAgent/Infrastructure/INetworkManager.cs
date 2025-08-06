using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Interface for secure network communications with TLS 1.3 encryption and JWT authentication
/// </summary>
public interface INetworkManager
{
    /// <summary>
    /// Makes a secure API call with automatic retry logic and JWT authentication
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint URL</param>
    /// <param name="data">Request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized response</returns>
    Task<T?> SecureApiCallAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a secure GET request
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized response</returns>
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a secure POST request
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint URL</param>
    /// <param name="data">Request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized response</returns>
    Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a secure PUT request
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint URL</param>
    /// <param name="data">Request data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized response</returns>
    Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a secure DELETE request
    /// </summary>
    /// <param name="endpoint">API endpoint URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the central server
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connectivity is available</returns>
    Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Establishes peer-to-peer connections with other nodes
    /// </summary>
    /// <param name="peerEndpoints">List of peer endpoints</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of successfully connected peers</returns>
    Task<IEnumerable<string>> EstablishPeerConnectionsAsync(IEnumerable<string> peerEndpoints, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates with the central server and obtains JWT token
    /// </summary>
    /// <param name="nodeId">Node identifier</param>
    /// <param name="credentials">Authentication credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if authentication successful</returns>
    Task<bool> AuthenticateAsync(string nodeId, Dictionary<string, string> credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the JWT token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token refresh successful</returns>
    Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current JWT token
    /// </summary>
    /// <returns>Current JWT token or null if not authenticated</returns>
    string? GetCurrentToken();

    /// <summary>
    /// Checks if the current token is valid and not expired
    /// </summary>
    /// <returns>True if token is valid</returns>
    bool IsTokenValid();

    /// <summary>
    /// Sets the base URL for API calls
    /// </summary>
    /// <param name="baseUrl">Base URL</param>
    void SetBaseUrl(string baseUrl);

    /// <summary>
    /// Sets custom headers for all requests
    /// </summary>
    /// <param name="headers">Custom headers</param>
    void SetCustomHeaders(Dictionary<string, string> headers);

    /// <summary>
    /// Gets network statistics
    /// </summary>
    /// <returns>Network statistics</returns>
    Task<NetworkStatistics> GetNetworkStatisticsAsync();

    /// <summary>
    /// Gets network status information
    /// </summary>
    /// <returns>Network status</returns>
    Task<NetworkStatus> GetNetworkStatusAsync();

    /// <summary>
    /// Makes a secure GET request (alias for GetAsync for backward compatibility)
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="endpoint">API endpoint URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized response</returns>
    Task<T?> SecureGetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when authentication fails
    /// </summary>
    event EventHandler<AuthenticationFailedEventArgs>? AuthenticationFailed;

    /// <summary>
    /// Event raised when network connectivity changes
    /// </summary>
    event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
}

/// <summary>
/// Network statistics model
/// </summary>
public class NetworkStatistics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public DateTime LastSuccessfulRequest { get; set; }
    public DateTime LastFailedRequest { get; set; }
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
}

/// <summary>
/// Network status model
/// </summary>
public class NetworkStatus
{
    public bool IsConnected { get; set; }
    public string? BaseUrl { get; set; }
    public DateTime LastConnectivityCheck { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Authentication failed event arguments
/// </summary>
public class AuthenticationFailedEventArgs : EventArgs
{
    public string Reason { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Connectivity changed event arguments
/// </summary>
public class ConnectivityChangedEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public string? Endpoint { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}