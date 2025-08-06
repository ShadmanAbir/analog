using System;
using System.Threading.Tasks;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Interface for managing all network communications with encryption and retry logic
/// </summary>
public interface INetworkManager
{
    /// <summary>
    /// Makes a secure API call with automatic retry logic
    /// </summary>
    /// <typeparam name="T">Expected return type</typeparam>
    /// <param name="endpoint">API endpoint to call</param>
    /// <param name="data">Data to send</param>
    /// <returns>Response data</returns>
    Task<T> SecureApiCallAsync<T>(string endpoint, object data);

    /// <summary>
    /// Makes a secure GET request
    /// </summary>
    /// <typeparam name="T">Expected return type</typeparam>
    /// <param name="endpoint">API endpoint to call</param>
    /// <returns>Response data</returns>
    Task<T> SecureGetAsync<T>(string endpoint);

    /// <summary>
    /// Tests connectivity to central services
    /// </summary>
    /// <returns>True if connectivity is available</returns>
    Task<bool> TestConnectivityAsync();

    /// <summary>
    /// Establishes peer-to-peer connections with other nodes
    /// </summary>
    void EstablishPeerConnections();

    /// <summary>
    /// Gets the current network status
    /// </summary>
    /// <returns>Network status information</returns>
    Task<NetworkStatus> GetNetworkStatusAsync();

    /// <summary>
    /// Configures network settings (firewall rules, etc.)
    /// </summary>
    Task ConfigureNetworkSettingsAsync();
}

/// <summary>
/// Represents the current network status
/// </summary>
public class NetworkStatus
{
    public bool IsConnected { get; set; }
    public bool CentralApiAvailable { get; set; }
    public int ConnectedPeers { get; set; }
    public DateTime LastConnectivityCheck { get; set; }
}