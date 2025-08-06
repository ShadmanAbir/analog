using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Implementation of network manager with secure communications and retry logic
/// </summary>
public class NetworkManager : INetworkManager
{
    private readonly ILogger<NetworkManager> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseApiUrl = "https://api.enterprise.local"; // TODO: Make configurable
    private readonly int _maxRetries = 3;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2);

    public NetworkManager(ILogger<NetworkManager> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        
        // Configure HTTP client for secure communications
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EnterpriseITAgent/1.0");
    }

    public async Task<T> SecureApiCallAsync<T>(string endpoint, object data)
    {
        var url = $"{_baseApiUrl}/{endpoint.TrimStart('/')}";
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        return await ExecuteWithRetryAsync(async () =>
        {
            _logger.LogDebug("Making API call to {Url}", url);
            
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseJson);
            
            _logger.LogDebug("API call successful");
            return result!;
        });
    }

    public async Task<T> SecureGetAsync<T>(string endpoint)
    {
        var url = $"{_baseApiUrl}/{endpoint.TrimStart('/')}";

        return await ExecuteWithRetryAsync(async () =>
        {
            _logger.LogDebug("Making GET request to {Url}", url);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<T>(responseJson);
            
            _logger.LogDebug("GET request successful");
            return result!;
        });
    }

    public async Task<bool> TestConnectivityAsync()
    {
        try
        {
            _logger.LogDebug("Testing connectivity to central services");
            
            // Test basic internet connectivity first
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 5000);
            
            if (reply.Status != System.Net.NetworkInformation.IPStatus.Success)
            {
                _logger.LogWarning("No internet connectivity detected");
                return false;
            }

            // TODO: Test connectivity to actual central API
            // For now, assume connectivity if internet is available
            _logger.LogDebug("Connectivity test passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connectivity test failed");
            return false;
        }
    }

    public void EstablishPeerConnections()
    {
        try
        {
            _logger.LogInformation("Establishing peer-to-peer connections");
            
            // TODO: Implement peer discovery and connection logic
            // This will be implemented in later tasks for distributed backup
            
            _logger.LogDebug("Peer connection establishment not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish peer connections");
        }
    }

    public async Task<NetworkStatus> GetNetworkStatusAsync()
    {
        var isConnected = await TestConnectivityAsync();
        
        return new NetworkStatus
        {
            IsConnected = isConnected,
            CentralApiAvailable = false, // TODO: Implement actual API health check
            ConnectedPeers = 0, // TODO: Implement peer counting
            LastConnectivityCheck = DateTime.UtcNow
        };
    }

    public async Task ConfigureNetworkSettingsAsync()
    {
        try
        {
            _logger.LogInformation("Configuring network settings");
            
            // TODO: Implement firewall rule configuration
            // TODO: Implement UPnP port mapping if needed
            // This will be implemented in later security tasks
            
            _logger.LogDebug("Network configuration not yet implemented");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure network settings");
            throw;
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        Exception? lastException = null;
        
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Operation failed on attempt {Attempt}/{MaxRetries}", attempt, _maxRetries);
                
                if (attempt < _maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                }
            }
        }
        
        _logger.LogError(lastException, "Operation failed after {MaxRetries} attempts", _maxRetries);
        throw lastException!;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}