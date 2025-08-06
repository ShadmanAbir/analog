using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EnterpriseITAgent.Models;
using Microsoft.Extensions.Logging;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Implementation of configuration management with local file and ERP integration
/// </summary>
public class ConfigurationManager : IConfigurationManager, IDisposable
{
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly INetworkManager _networkManager;
    private readonly string _configFilePath = "userconfig.json";
    private readonly string _backupConfigFilePath = "userconfig.backup.json";
    private Configuration? _currentConfiguration;
    private FileSystemWatcher? _configWatcher;
    private readonly object _configLock = new object();

    public ConfigurationManager(ILogger<ConfigurationManager> logger, INetworkManager networkManager)
    {
        _logger = logger;
        _networkManager = networkManager;
    }

    public async Task<Configuration> LoadConfigurationAsync()
    {
        _logger.LogInformation("Loading configuration...");

        lock (_configLock)
        {
            if (_currentConfiguration != null)
            {
                _logger.LogDebug("Returning cached configuration");
                return Task.FromResult(_currentConfiguration).Result;
            }
        }

        try
        {
            // Try to fetch from ERP first
            if (await TryFetchFromErpAsync())
            {
                _logger.LogInformation("Configuration loaded from ERP system");
                await ValidateAndApplyConfiguration(_currentConfiguration!);
                return _currentConfiguration!;
            }

            // Fall back to local file
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                var config = JsonSerializer.Deserialize<Configuration>(json);
                
                if (config != null && ValidateConfiguration(config))
                {
                    lock (_configLock)
                    {
                        _currentConfiguration = config;
                    }
                    _logger.LogInformation("Configuration loaded from local file");
                }
                else
                {
                    _logger.LogWarning("Invalid configuration in local file, using backup or default");
                    await LoadBackupOrCreateDefault();
                }
            }
            else
            {
                // Create default configuration
                await LoadBackupOrCreateDefault();
            }

            return _currentConfiguration!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            await LoadBackupOrCreateDefault();
            return _currentConfiguration!;
        }
    }

    public async Task ApplyConfigurationAsync(Configuration config)
    {
        _logger.LogInformation("Applying configuration for node {NodeId}", config.NodeId);
        
        try
        {
            // Validate configuration before applying
            if (!ValidateConfiguration(config))
            {
                throw new InvalidOperationException("Configuration validation failed");
            }

            // Create backup of current configuration
            if (_currentConfiguration != null)
            {
                await SaveConfigurationBackupAsync(_currentConfiguration);
            }

            // Update timestamp
            config.LastUpdated = DateTime.UtcNow;

            // Apply configuration
            await ValidateAndApplyConfiguration(config);
            await SaveConfigurationAsync(config);
            
            lock (_configLock)
            {
                _currentConfiguration = config;
            }
            
            _logger.LogInformation("Configuration applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply configuration");
            throw;
        }
    }

    public async Task<bool> TryFetchFromErpAsync()
    {
        try
        {
            if (!await _networkManager.TestConnectivityAsync())
            {
                _logger.LogWarning("No network connectivity, cannot fetch from ERP");
                return false;
            }

            var nodeId = GenerateNodeId();
            _logger.LogDebug("Attempting to fetch configuration for node {NodeId}", nodeId);

            try
            {
                // Attempt to fetch configuration from ERP API
                var config = await _networkManager.SecureGetAsync<Configuration>($"/api/configuration/{nodeId}");
                
                if (config != null && ValidateConfiguration(config))
                {
                    lock (_configLock)
                    {
                        _currentConfiguration = config;
                    }
                    _logger.LogInformation("Successfully fetched configuration from ERP for node {NodeId}", nodeId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Invalid configuration received from ERP");
                    return false;
                }
            }
            catch (Exception apiEx)
            {
                _logger.LogWarning(apiEx, "ERP API call failed, falling back to local configuration");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching configuration from ERP");
            return false;
        }
    }

    public void WatchForConfigChanges()
    {
        try
        {
            // Dispose existing watcher if any
            _configWatcher?.Dispose();

            var directory = Path.GetDirectoryName(Path.GetFullPath(_configFilePath)) ?? ".";
            var fileName = Path.GetFileName(_configFilePath);

            _configWatcher = new FileSystemWatcher(directory, fileName);
            _configWatcher.Changed += OnConfigFileChanged;
            _configWatcher.Created += OnConfigFileChanged;
            _configWatcher.Renamed += OnConfigFileChanged;
            _configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime;
            _configWatcher.EnableRaisingEvents = true;
            
            _logger.LogInformation("Started watching for configuration changes at {FilePath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start configuration file watcher");
        }
    }

    public async Task SaveConfigurationAsync(Configuration config)
    {
        try
        {
            // Validate before saving
            if (!ValidateConfiguration(config))
            {
                throw new InvalidOperationException("Cannot save invalid configuration");
            }

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(config, options);
            
            // Write to temporary file first, then rename for atomic operation
            var tempFilePath = _configFilePath + ".tmp";
            await File.WriteAllTextAsync(tempFilePath, json);
            
            // Atomic rename
            if (File.Exists(_configFilePath))
            {
                File.Replace(tempFilePath, _configFilePath, null);
            }
            else
            {
                File.Move(tempFilePath, _configFilePath);
            }
            
            _logger.LogDebug("Configuration saved to {FilePath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration");
            throw;
        }
    }

    private Configuration CreateDefaultConfiguration()
    {
        var nodeId = GenerateNodeId();
        
        return new Configuration
        {
            NodeId = nodeId,
            Email = new EmailConfiguration(),
            Backup = new BackupConfiguration
            {
                BackupPaths = new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) },
                EncryptionKey = GenerateEncryptionKey()
            },
            Security = new SecurityConfiguration(),
            CustomSettings = new Dictionary<string, object>(),
            Version = "1.0.0",
            LastUpdated = DateTime.UtcNow
        };
    }

    private string GenerateNodeId()
    {
        // Use MAC address + username for unique identification as per requirements
        var macAddress = GetMacAddress();
        var username = Environment.UserName;
        var machineId = $"{macAddress}_{username}";
        
        // Hash to create a consistent but shorter ID
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
        var hashString = Convert.ToHexString(hashBytes)[..16]; // Take first 16 characters
        
        return $"node_{hashString.ToLowerInvariant()}";
    }

    private string GetMacAddress()
    {
        try
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up && 
                                     ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            
            return networkInterface?.GetPhysicalAddress().ToString() ?? "unknown";
        }
        catch
        {
            return Environment.MachineName;
        }
    }

    private string GenerateEncryptionKey()
    {
        // Generate a 32-character encryption key
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32];
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    private bool ValidateConfiguration(Configuration config)
    {
        try
        {
            var validationResults = ConfigurationValidator.ValidateConfiguration(config);
            var isValid = !validationResults.Any();
            
            if (!isValid)
            {
                var errors = validationResults.Select(r => r.ErrorMessage).ToList();
                _logger.LogWarning("Configuration validation failed: {Errors}", string.Join(", ", errors));
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration validation");
            return false;
        }
    }

    private async Task ValidateAndApplyConfiguration(Configuration config)
    {
        // This method will contain logic to apply configuration settings to the system
        // For now, we'll just log the application
        _logger.LogInformation("Applying configuration settings...");
        
        if (config.Email != null)
        {
            _logger.LogDebug("Email configuration: IMAP={ImapServer}:{ImapPort}, SMTP={SmtpServer}:{SmtpPort}", 
                config.Email.ImapServer, config.Email.ImapPort, config.Email.SmtpServer, config.Email.SmtpPort);
        }
        
        if (config.Printers != null && config.Printers.Length > 0)
        {
            _logger.LogDebug("Printer configurations: {PrinterCount} printers configured", config.Printers.Length);
        }
        
        if (config.Backup != null)
        {
            _logger.LogDebug("Backup configuration: {PathCount} paths, {MaxSize}GB max size", 
                config.Backup.BackupPaths?.Length ?? 0, config.Backup.MaxBackupSizeGB);
        }
        
        // TODO: Implement actual system configuration application in later tasks
        await Task.CompletedTask;
    }

    private async Task LoadBackupOrCreateDefault()
    {
        try
        {
            if (File.Exists(_backupConfigFilePath))
            {
                var backupJson = await File.ReadAllTextAsync(_backupConfigFilePath);
                var backupConfig = JsonSerializer.Deserialize<Configuration>(backupJson);
                
                if (backupConfig != null && ValidateConfiguration(backupConfig))
                {
                    lock (_configLock)
                    {
                        _currentConfiguration = backupConfig;
                    }
                    _logger.LogInformation("Configuration loaded from backup file");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load backup configuration");
        }

        // Create default configuration
        var defaultConfig = CreateDefaultConfiguration();
        lock (_configLock)
        {
            _currentConfiguration = defaultConfig;
        }
        await SaveConfigurationAsync(defaultConfig);
        _logger.LogInformation("Created default configuration");
    }

    private async Task SaveConfigurationBackupAsync(Configuration config)
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(_backupConfigFilePath, json);
            _logger.LogDebug("Configuration backup saved to {FilePath}", _backupConfigFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save configuration backup");
        }
    }

    private async void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Add a small delay to ensure file write is complete
            await Task.Delay(500);
            
            _logger.LogInformation("Configuration file changed, reloading...");
            
            // Clear cached configuration to force reload
            lock (_configLock)
            {
                _currentConfiguration = null;
            }
            
            await LoadConfigurationAsync();
            _logger.LogInformation("Configuration reloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload configuration after file change");
        }
    }

    public void Dispose()
    {
        _configWatcher?.Dispose();
    }
}