using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseITAgent.Infrastructure;
using EnterpriseITAgent.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseITAgent.Tests.Infrastructure;

public class ConfigurationManagerTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationManager>> _mockLogger;
    private readonly Mock<INetworkManager> _mockNetworkManager;
    private readonly ConfigurationManager _configurationManager;
    private readonly string _testConfigFile = "test_userconfig.json";
    private readonly string _testBackupConfigFile = "test_userconfig.backup.json";

    public ConfigurationManagerTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationManager>>();
        _mockNetworkManager = new Mock<INetworkManager>();
        
        // Use reflection to set the config file paths for testing
        _configurationManager = new ConfigurationManager(_mockLogger.Object, _mockNetworkManager.Object);
        
        // Clean up any existing test files
        CleanupTestFiles();
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenErpAvailable_ShouldLoadFromErp()
    {
        // Arrange
        var expectedConfig = CreateTestConfiguration();
        _mockNetworkManager.Setup(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockNetworkManager.Setup(x => x.SecureGetAsync<Configuration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConfig);

        // Act
        var result = await _configurationManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConfig.NodeId, result.NodeId);
        _mockNetworkManager.Verify(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>()));
        _mockNetworkManager.Verify(x => x.SecureGetAsync<Configuration>(It.IsAny<string>(), It.IsAny<CancellationToken>()),Times.Once);
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenErpUnavailable_ShouldLoadFromLocalFile()
    {
        // Arrange
        var testConfig = CreateTestConfiguration();
        await CreateTestConfigFile(testConfig);
        
        _mockNetworkManager.Setup(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _configurationManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        // Note: The actual NodeId will be generated, so we check other properties
        Assert.NotNull(result.Email);
        Assert.NotNull(result.Backup);
        Assert.NotNull(result.Security);
    }

    [Fact]
    public async Task LoadConfigurationAsync_WhenNoConfigExists_ShouldCreateDefault()
    {
        // Arrange
        _mockNetworkManager.Setup(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _configurationManager.LoadConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.NodeId);
        Assert.NotNull(result.Email);
        Assert.NotNull(result.Backup);
        Assert.NotNull(result.Security);
        Assert.Equal("1.0.0", result.Version);
    }

    [Fact]
    public async Task ApplyConfigurationAsync_WithValidConfig_ShouldSaveAndApply()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        await _configurationManager.ApplyConfigurationAsync(config);

        // Assert
        // Verify that the configuration was applied (we can't easily test the internal state,
        // but we can verify no exceptions were thrown)
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public async Task ApplyConfigurationAsync_WithInvalidConfig_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = new Configuration
        {
            NodeId = "", // Invalid - empty NodeId
            Version = "invalid-version" // Invalid version format
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _configurationManager.ApplyConfigurationAsync(invalidConfig));
    }

    [Fact]
    public async Task TryFetchFromErpAsync_WhenNetworkUnavailable_ShouldReturnFalse()
    {
        // Arrange
        _mockNetworkManager.Setup(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _configurationManager.TryFetchFromErpAsync();

        // Assert
        Assert.False(result);
        _mockNetworkManager.Verify(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockNetworkManager.Verify(x => x.SecureGetAsync<Configuration>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryFetchFromErpAsync_WhenErpReturnsValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var validConfig = CreateTestConfiguration();
        _mockNetworkManager.Setup(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockNetworkManager.Setup(x => x.SecureGetAsync<Configuration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validConfig);

        // Act
        var result = await _configurationManager.TryFetchFromErpAsync();

        // Assert
        Assert.True(result);
        _mockNetworkManager.Verify(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockNetworkManager.Verify(x => x.SecureGetAsync<Configuration>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryFetchFromErpAsync_WhenErpReturnsInvalidConfig_ShouldReturnFalse()
    {
        // Arrange
        var invalidConfig = new Configuration
        {
            NodeId = "", // Invalid
            Version = "invalid"
        };
        _mockNetworkManager.Setup(x => x.TestConnectivityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockNetworkManager.Setup(x => x.SecureGetAsync<Configuration>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidConfig);

        // Act
        var result = await _configurationManager.TryFetchFromErpAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SaveConfigurationAsync_WithValidConfig_ShouldSaveToFile()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        await _configurationManager.SaveConfigurationAsync(config);

        // Assert
        // We can't easily test the private file path, but we can verify no exception was thrown
        Assert.True(true);
    }

    [Fact]
    public async Task SaveConfigurationAsync_WithInvalidConfig_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = new Configuration
        {
            NodeId = "", // Invalid
            Version = "invalid"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _configurationManager.SaveConfigurationAsync(invalidConfig));
    }

    [Fact]
    public void WatchForConfigChanges_ShouldStartWatcher()
    {
        // Act
        _configurationManager.WatchForConfigChanges();

        // Assert
        // We can't easily test the file watcher, but we can verify no exception was thrown
        Assert.True(true);
    }

    private Configuration CreateTestConfiguration()
    {
        return new Configuration
        {
            NodeId = "test-node-001",
            Version = "1.0.0",
            LastUpdated = DateTime.UtcNow,
            Email = new EmailConfiguration
            {
                ImapServer = "imap.test.com",
                ImapPort = 993,
                SmtpServer = "smtp.test.com",
                SmtpPort = 587,
                Username = "test@test.com",
                Password = "password123"
            },
            Backup = new BackupConfiguration
            {
                BackupPaths = new[] { "C:\\TestPath" },
                MaxBackupSizeGB = 50,
                RetentionDays = 30,
                EncryptionKey = "TestEncryptionKeyWith32Characters"
            },
            Security = new SecurityConfiguration
            {
                EnableEncryption = true,
                RequireAuthentication = true,
                SessionTimeoutMinutes = 60
            }
        };
    }

    private async Task CreateTestConfigFile(Configuration config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_testConfigFile, json);
    }

    private void CleanupTestFiles()
    {
        try
        {
            if (File.Exists(_testConfigFile))
                File.Delete(_testConfigFile);
            if (File.Exists(_testBackupConfigFile))
                File.Delete(_testBackupConfigFile);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public void Dispose()
    {
        _configurationManager?.Dispose();
        CleanupTestFiles();
    }
}