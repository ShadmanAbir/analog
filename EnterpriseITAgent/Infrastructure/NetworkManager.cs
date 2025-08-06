using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Secure network manager with TLS 1.3 encryption, JWT authentication, and retry logic
/// </summary>
public class NetworkManager : INetworkManager, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NetworkManager> _logger;
    private readonly ILoggingService _loggingService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly NetworkStatistics _statistics;
    private readonly object _statisticsLock = new();
    
    private string? _currentToken;
    private DateTime _tokenExpiry;
    private string _baseUrl = string.Empty;
    private readonly Dictionary<string, string> _customHeaders = new();
    private bool _disposed;

    public event EventHandler<AuthenticationFailedEventArgs>? AuthenticationFailed;
    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

    public NetworkManager(ILogger<NetworkManager> logger, ILoggingService loggingService)
        : this(logger, loggingService, null)
    {
    }

    // Constructor for testing with custom HttpClient
    protected NetworkManager(ILogger<NetworkManager> logger, ILoggingService loggingService, HttpClient? httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        
        _statistics = new NetworkStatistics();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
        else
        {
            // Configure HttpClient with TLS 1.3 and security settings
            var handler = new HttpClientHandler()
            {
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
                ServerCertificateCustomValidationCallback = ValidateServerCertificate,
                UseCookies = false, // Disable cookies for security
                MaxConnectionsPerServer = 10 // Limit connections per server
            };

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "EnterpriseITAgent/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        }
        
        _loggingService.LogInfo("NetworkManager initialized with TLS 1.3 support", "NetworkManager");
    }

    public async Task<T?> SecureApiCallAsync<T>(string endpoint, object? data = null, CancellationToken cancellationToken = default)
    {
        if (data == null)
        {
            return await GetAsync<T>(endpoint, cancellationToken);
        }
        else
        {
            return await PostAsync<T>(endpoint, data, cancellationToken);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync<T>(async () =>
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var request = CreateRequest(HttpMethod.Get, endpoint);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            return await ProcessResponseAsync<T>(response);
        }, endpoint);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync<T>(async () =>
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var request = CreateRequest(HttpMethod.Post, endpoint);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            return await ProcessResponseAsync<T>(response);
        }, endpoint);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync<T>(async () =>
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var request = CreateRequest(HttpMethod.Put, endpoint);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            return await ProcessResponseAsync<T>(response);
        }, endpoint);
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            
            var request = CreateRequest(HttpMethod.Delete, endpoint);
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            UpdateStatistics(true, response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}");
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            UpdateStatistics(false, ex.Message);
            _loggingService.LogError(ex, $"DELETE request failed for endpoint: {endpoint}", "NetworkManager");
            return false;
        }
    }

    public async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "/api/health");
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            var isConnected = response.IsSuccessStatusCode;
            OnConnectivityChanged(isConnected, _baseUrl);
            
            _loggingService.LogInfo($"Connectivity test result: {isConnected}", "NetworkManager");
            
            return isConnected;
        }
        catch (Exception ex)
        {
            OnConnectivityChanged(false, _baseUrl);
            _loggingService.LogError(ex, "Connectivity test failed", "NetworkManager");
            return false;
        }
    }

    public async Task<IEnumerable<string>> EstablishPeerConnectionsAsync(IEnumerable<string> peerEndpoints, CancellationToken cancellationToken = default)
    {
        var connectedPeers = new List<string>();
        
        foreach (var peer in peerEndpoints)
        {
            try
            {
                var originalBaseUrl = _baseUrl;
                SetBaseUrl(peer);
                
                var isConnected = await TestConnectivityAsync(cancellationToken);
                if (isConnected)
                {
                    connectedPeers.Add(peer);
                    _loggingService.LogInfo($"Successfully connected to peer: {peer}", "NetworkManager");
                }
                
                SetBaseUrl(originalBaseUrl);
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, $"Failed to connect to peer: {peer}", "NetworkManager");
            }
        }
        
        return connectedPeers;
    }

    public async Task<bool> AuthenticateAsync(string nodeId, Dictionary<string, string> credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            var authData = new
            {
                NodeId = nodeId,
                Credentials = credentials,
                Timestamp = DateTime.UtcNow,
                ClientVersion = "1.0.0",
                Platform = Environment.OSVersion.Platform.ToString()
            };
            
            var request = CreateRequest(HttpMethod.Post, "/api/auth/login");
            var json = JsonSerializer.Serialize(authData, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent, _jsonOptions);
                
                if (authResponse?.Token != null && ValidateJwtToken(authResponse.Token))
                {
                    _currentToken = authResponse.Token;
                    _tokenExpiry = authResponse.ExpiresAt;
                    
                    _loggingService.LogInfo($"Authentication successful for node: {nodeId}. Token expires at: {_tokenExpiry:yyyy-MM-dd HH:mm:ss} UTC", "NetworkManager");
                    return true;
                }
                else
                {
                    OnAuthenticationFailed("Invalid JWT token received from server", null);
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                OnAuthenticationFailed($"Authentication failed with status {response.StatusCode}: {errorContent}", null);
                return false;
            }
        }
        catch (Exception ex)
        {
            OnAuthenticationFailed("Authentication request failed", ex);
            _loggingService.LogError(ex, $"Authentication failed for node: {nodeId}", "NetworkManager");
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentToken))
            {
                _loggingService.LogWarning("Cannot refresh token: no current token available", "NetworkManager");
                return false;
            }
            
            var refreshData = new
            {
                Token = _currentToken,
                Timestamp = DateTime.UtcNow
            };
            
            var request = CreateRequest(HttpMethod.Post, "/api/auth/refresh");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);
            
            var json = JsonSerializer.Serialize(refreshData, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent, _jsonOptions);
                
                if (authResponse?.Token != null && ValidateJwtToken(authResponse.Token))
                {
                    var oldExpiry = _tokenExpiry;
                    _currentToken = authResponse.Token;
                    _tokenExpiry = authResponse.ExpiresAt;
                    
                    _loggingService.LogInfo($"Token refresh successful. Old expiry: {oldExpiry:yyyy-MM-dd HH:mm:ss}, New expiry: {_tokenExpiry:yyyy-MM-dd HH:mm:ss} UTC", "NetworkManager");
                    return true;
                }
                else
                {
                    OnAuthenticationFailed("Invalid JWT token received during refresh", null);
                    return false;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                OnAuthenticationFailed($"Token refresh failed with status {response.StatusCode}: {errorContent}", null);
                return false;
            }
        }
        catch (Exception ex)
        {
            OnAuthenticationFailed("Token refresh request failed", ex);
            _loggingService.LogError(ex, "Token refresh failed", "NetworkManager");
            return false;
        }
    }

    public string? GetCurrentToken()
    {
        return _currentToken;
    }

    public bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(_currentToken) && DateTime.UtcNow < _tokenExpiry;
    }

    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl?.TrimEnd('/') ?? string.Empty;
        _loggingService.LogInfo($"Base URL set to: {_baseUrl}", "NetworkManager");
    }

    public void SetCustomHeaders(Dictionary<string, string> headers)
    {
        _customHeaders.Clear();
        foreach (var header in headers)
        {
            _customHeaders[header.Key] = header.Value;
        }
        
        _loggingService.LogInfo($"Custom headers set: {headers.Count} headers", "NetworkManager");
    }

    public async Task<NetworkStatistics> GetNetworkStatisticsAsync()
    {
        return await Task.FromResult(new NetworkStatistics
        {
            TotalRequests = _statistics.TotalRequests,
            SuccessfulRequests = _statistics.SuccessfulRequests,
            FailedRequests = _statistics.FailedRequests,
            AverageResponseTimeMs = _statistics.AverageResponseTimeMs,
            LastSuccessfulRequest = _statistics.LastSuccessfulRequest,
            LastFailedRequest = _statistics.LastFailedRequest,
            ErrorCounts = new Dictionary<string, int>(_statistics.ErrorCounts)
        });
    }

    public async Task<NetworkStatus> GetNetworkStatusAsync()
    {
        var startTime = DateTime.UtcNow;
        var isConnected = await TestConnectivityAsync();
        var responseTime = DateTime.UtcNow - startTime;

        return new NetworkStatus
        {
            IsConnected = isConnected,
            BaseUrl = _baseUrl,
            LastConnectivityCheck = DateTime.UtcNow,
            ResponseTime = responseTime,
            ErrorMessage = isConnected ? null : "Connectivity test failed"
        };
    }

    public async Task<T?> SecureGetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await GetAsync<T>(endpoint, cancellationToken);
    }

    private void ConfigureRetryPolicy()
    {
        // Retry policy is handled by Polly in ExecuteWithRetryAsync method
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> operation, string endpoint)
    {
        // Enhanced retry policy with exponential backoff and jitter
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<SocketException>()
            .Or<TimeoutException>()
            .Or<UnauthorizedAccessException>() // Retry on auth failures (token might need refresh)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => 
                {
                    // Exponential backoff with jitter: base delay * 2^attempt + random jitter
                    var baseDelay = TimeSpan.FromSeconds(1);
                    var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)); // Add up to 1 second jitter
                    return exponentialDelay.Add(jitter);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var exceptionType = outcome.GetType().Name;
                    _loggingService.LogWarning(
                        $"Retry {retryCount}/3 for endpoint {endpoint} in {timespan.TotalSeconds:F2}s due to {exceptionType}: {outcome.Message}", 
                        "NetworkManager");
                });

        try
        {
            var startTime = DateTime.UtcNow;
            var result = await retryPolicy.ExecuteAsync(operation);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            UpdateStatistics(true, null, duration);
            return result;
        }
        catch (Exception ex)
        {
            UpdateStatistics(false, ex.Message);
            _loggingService.LogError(ex, $"Request failed after {3} retries for endpoint: {endpoint}", "NetworkManager");
            throw;
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var url = string.IsNullOrEmpty(_baseUrl) ? endpoint : $"{_baseUrl}{endpoint}";
        var request = new HttpRequestMessage(method, url);
        
        // Add custom headers
        foreach (var header in _customHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        // Add JWT token if available
        if (!string.IsNullOrEmpty(_currentToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _currentToken);
        }
        
        return request;
    }

    private async Task<T?> ProcessResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return default(T);
            }
            
            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {errorContent}");
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        // Check if token is valid (not expired and properly formatted)
        if (!IsTokenValid())
        {
            // Try to refresh token first if we have one
            if (!string.IsNullOrEmpty(_currentToken))
            {
                _loggingService.LogInfo("Token expired or invalid, attempting refresh", "NetworkManager");
                var refreshed = await RefreshTokenAsync(cancellationToken);
                if (!refreshed)
                {
                    // Clear invalid token
                    _currentToken = null;
                    _tokenExpiry = DateTime.MinValue;
                    throw new UnauthorizedAccessException("Token expired and refresh failed. Re-authentication required.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("No valid authentication token available. Authentication required.");
            }
        }
        
        // Additional check: if token expires within 5 minutes, proactively refresh
        if (DateTime.UtcNow.AddMinutes(5) >= _tokenExpiry && !string.IsNullOrEmpty(_currentToken))
        {
            _loggingService.LogInfo("Token expires soon, proactively refreshing", "NetworkManager");
            await RefreshTokenAsync(cancellationToken);
        }
    }

    private static bool ValidateServerCertificate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        // If no SSL policy errors, certificate is valid
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }
        
        // Log certificate validation issues for debugging
        var logger = request.Options.TryGetValue(new HttpRequestOptionsKey<ILogger>("Logger"), out var loggerValue) ? loggerValue : null;
        
        if (certificate == null)
        {
            logger?.LogWarning("Server certificate is null");
            return false;
        }
        
        // Check for specific SSL policy errors
        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            logger?.LogWarning("Remote certificate not available");
            return false;
        }
        
        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            logger?.LogWarning($"Certificate name mismatch. Subject: {certificate.Subject}, Request URI: {request.RequestUri}");
            // In development, you might want to allow this, but in production it should be rejected
            #if DEBUG
            return true;
            #else
            return false;
            #endif
        }
        
        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            logger?.LogWarning($"Certificate chain errors. Chain status: {chain?.ChainStatus?.FirstOrDefault().StatusInformation}");
            
            // Check if it's just a self-signed certificate (common in development)
            if (chain?.ChainStatus?.Any(status => status.Status == X509ChainStatusFlags.UntrustedRoot) == true)
            {
                #if DEBUG
                logger?.LogWarning("Accepting self-signed certificate in debug mode");
                return true;
                #else
                return false;
                #endif
            }
        }
        
        // Default to rejecting invalid certificates in production
        logger?.LogError($"Certificate validation failed with errors: {sslPolicyErrors}");
        return false;
    }

    private void UpdateStatistics(bool success, string? error = null, double? responseTimeMs = null)
    {
        lock (_statisticsLock)
        {
            _statistics.TotalRequests++;
            
            if (success)
            {
                _statistics.SuccessfulRequests++;
                _statistics.LastSuccessfulRequest = DateTime.UtcNow;
            }
            else
            {
                _statistics.FailedRequests++;
                _statistics.LastFailedRequest = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(error))
                {
                    _statistics.ErrorCounts[error] = _statistics.ErrorCounts.GetValueOrDefault(error, 0) + 1;
                }
            }
            
            if (responseTimeMs.HasValue)
            {
                // Simple moving average calculation
                _statistics.AverageResponseTimeMs = (_statistics.AverageResponseTimeMs + responseTimeMs.Value) / 2;
            }
        }
    }

    private void OnAuthenticationFailed(string reason, Exception? exception)
    {
        AuthenticationFailed?.Invoke(this, new AuthenticationFailedEventArgs
        {
            Reason = reason,
            Exception = exception
        });
    }

    private void OnConnectivityChanged(bool isConnected, string? endpoint)
    {
        ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs
        {
            IsConnected = isConnected,
            Endpoint = endpoint
        });
    }

    private bool ValidateJwtToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // Basic validation - check if token can be parsed
            if (!handler.CanReadToken(token))
            {
                _loggingService.LogWarning("Received token is not a valid JWT format", "NetworkManager");
                return false;
            }
            
            var jwtToken = handler.ReadJwtToken(token);
            
            // Check if token has required claims
            if (string.IsNullOrEmpty(jwtToken.Subject))
            {
                _loggingService.LogWarning("JWT token missing subject claim", "NetworkManager");
                return false;
            }
            
            // Check if token is not expired (with 5 minute buffer)
            var expiry = jwtToken.ValidTo;
            if (expiry <= DateTime.UtcNow.AddMinutes(5))
            {
                _loggingService.LogWarning($"JWT token is expired or expires soon. Expiry: {expiry:yyyy-MM-dd HH:mm:ss} UTC", "NetworkManager");
                return false;
            }
            
            _loggingService.LogInfo($"JWT token validated successfully. Subject: {jwtToken.Subject}, Expiry: {expiry:yyyy-MM-dd HH:mm:ss} UTC", "NetworkManager");
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError(ex, "Failed to validate JWT token", "NetworkManager");
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Authentication response model
/// </summary>
internal class AuthenticationResponse
{
    public string? Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
}