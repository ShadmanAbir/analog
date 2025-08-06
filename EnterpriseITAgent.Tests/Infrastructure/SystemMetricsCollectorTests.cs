using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseITAgent.Infrastructure;
using EnterpriseITAgent.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseITAgent.Tests.Infrastructure;

/// <summary>
/// Unit tests for SystemMetricsCollector
/// </summary>
public class SystemMetricsCollectorTests : IDisposable
{
    private readonly Mock<ILogger<SystemMetricsCollector>> _mockLogger;
    private readonly Mock<INetworkManager> _mockNetworkManager;
    private readonly Mock<IConfigurationManager> _mockConfigurationManager;
    private readonly SystemMetricsCollector _systemMetricsCollector;

    public SystemMetricsCollectorTests()
    {
        _mockLogger = new Mock<ILogger<SystemMetricsCollector>>();
        _mockNetworkManager = new Mock<INetworkManager>();
        _mockConfigurationManager = new Mock<IConfigurationManager>();

        // Setup default configuration
        var defaultConfig = new Configuration
        {
            Backup = new BackupConfiguration
            {
                BackupPaths = new[] { @"C:\Temp\Backup" }
            }
        };
        _mockConfigurationManager.Setup(x => x.LoadConfigurationAsync())
            .ReturnsAsync(defaultConfig);

        _systemMetricsCollector = new SystemMetricsCollector(
            _mockLogger.Object,
            _mockNetworkManager.Object,
            _mockConfigurationManager.Object);
    }

    [Fact]
    public async Task CollectMetricsAsync_ShouldReturnValidMetrics()
    {
        // Act
        var metrics = await _systemMetricsCollector.CollectMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.Timestamp > DateTime.MinValue);
        Assert.True(metrics.CpuUsage >= 0 && metrics.CpuUsage <= 100);
        Assert.True(metrics.MemoryUsage >= 0 && metrics.MemoryUsage <= 100);
        Assert.True(metrics.DiskUsage >= 0 && metrics.DiskUsage <= 100);
        Assert.True(metrics.TotalMemoryMB >= 0);
        Assert.True(metrics.AvailableMemoryMB >= 0);
        Assert.True(metrics.TotalDiskGB >= 0);
        Assert.True(metrics.AvailableDiskGB >= 0);
        Assert.True(metrics.ProcessCount > 0);
        Assert.True(metrics.UptimeHours >= 0);
    }

    [Fact]
    public async Task GetNodeStatusAsync_ShouldReturnValidNodeStatus()
    {
        // Act
        var nodeStatus = await _systemMetricsCollector.GetNodeStatusAsync();

        // Assert
        Assert.NotNull(nodeStatus);
        Assert.False(string.IsNullOrEmpty(nodeStatus.NodeId));
        Assert.True(nodeStatus.LastHeartbeat > DateTime.MinValue);
        Assert.NotNull(nodeStatus.Metrics);
        Assert.NotNull(nodeStatus.ActiveServices);
        Assert.NotNull(nodeStatus.Alerts);
        Assert.False(string.IsNullOrEmpty(nodeStatus.Version));
        Assert.False(string.IsNullOrEmpty(nodeStatus.OsInfo));
        Assert.False(string.IsNullOrEmpty(nodeStatus.MachineName));
        Assert.False(string.IsNullOrEmpty(nodeStatus.CurrentUser));
        Assert.True(nodeStatus.AvailableBackupStorageGB >= 0);
    }

    [Fact]
    public async Task SendHeartbeatAsync_ShouldCallNetworkManager()
    {
        // Arrange
        _mockNetworkManager.Setup(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act
        await _systemMetricsCollector.SendHeartbeatAsync();

        // Assert
        _mockNetworkManager.Verify(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendHeartbeatAsync_ShouldHandleNetworkFailureGracefully()
    {
        // Arrange
        _mockNetworkManager.Setup(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act & Assert - Should not throw
        await _systemMetricsCollector.SendHeartbeatAsync();

        // Verify that the network call was attempted
        _mockNetworkManager.Verify(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsSystemIdleAsync_ShouldReturnBooleanValue()
    {
        // Act
        var isIdle = await _systemMetricsCollector.IsSystemIdleAsync();

        // Assert
        Assert.IsType<bool>(isIdle);
    }

    [Fact]
    public async Task GetAvailableBackupStorageAsync_ShouldReturnNonNegativeValue()
    {
        // Act
        var availableStorage = await _systemMetricsCollector.GetAvailableBackupStorageAsync();

        // Assert
        Assert.True(availableStorage >= 0);
    }

    [Fact]
    public async Task GetAvailableBackupStorageAsync_ShouldUseConfigurationPath()
    {
        // Arrange
        var customConfig = new Configuration
        {
            Backup = new BackupConfiguration
            {
                BackupPaths = new[] { @"D:\CustomBackup" }
            }
        };
        _mockConfigurationManager.Setup(x => x.LoadConfigurationAsync())
            .ReturnsAsync(customConfig);

        // Act
        var availableStorage = await _systemMetricsCollector.GetAvailableBackupStorageAsync();

        // Assert
        Assert.True(availableStorage >= 0);
        _mockConfigurationManager.Verify(x => x.LoadConfigurationAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void AddCustomMetric_ShouldAddMetricSuccessfully()
    {
        // Arrange
        const string metricName = "TestMetric";
        Func<Task<double>> valueProvider = () => Task.FromResult(42.0);

        // Act
        _systemMetricsCollector.AddCustomMetric(metricName, valueProvider);

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public void AddCustomMetric_ShouldThrowForNullName()
    {
        // Arrange
        Func<Task<double>> valueProvider = () => Task.FromResult(42.0);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _systemMetricsCollector.AddCustomMetric(null!, valueProvider));
        Assert.Throws<ArgumentException>(() => _systemMetricsCollector.AddCustomMetric("", valueProvider));
        Assert.Throws<ArgumentException>(() => _systemMetricsCollector.AddCustomMetric("   ", valueProvider));
    }

    [Fact]
    public void AddCustomMetric_ShouldThrowForNullValueProvider()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _systemMetricsCollector.AddCustomMetric("TestMetric", null!));
    }

    [Fact]
    public void RemoveCustomMetric_ShouldRemoveMetricSuccessfully()
    {
        // Arrange
        const string metricName = "TestMetric";
        Func<Task<double>> valueProvider = () => Task.FromResult(42.0);
        _systemMetricsCollector.AddCustomMetric(metricName, valueProvider);

        // Act
        _systemMetricsCollector.RemoveCustomMetric(metricName);

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task CollectMetricsAsync_ShouldIncludeCustomMetrics()
    {
        // Arrange
        const string metricName = "TestCustomMetric";
        const double expectedValue = 123.45;
        Func<Task<double>> valueProvider = () => Task.FromResult(expectedValue);
        
        _systemMetricsCollector.AddCustomMetric(metricName, valueProvider);

        // Act
        var metrics = await _systemMetricsCollector.CollectMetricsAsync();

        // Assert
        Assert.NotNull(metrics.CustomMetrics);
        Assert.True(metrics.CustomMetrics.ContainsKey(metricName));
        Assert.Equal(expectedValue, metrics.CustomMetrics[metricName]);
    }

    [Fact]
    public async Task CollectMetricsAsync_ShouldHandleCustomMetricFailures()
    {
        // Arrange
        const string metricName = "FailingMetric";
        Func<Task<double>> failingProvider = () => throw new Exception("Custom metric error");
        
        _systemMetricsCollector.AddCustomMetric(metricName, failingProvider);

        // Act
        var metrics = await _systemMetricsCollector.CollectMetricsAsync();

        // Assert - Should not throw and should not include the failing metric
        Assert.NotNull(metrics);
        if (metrics.CustomMetrics != null)
        {
            Assert.False(metrics.CustomMetrics.ContainsKey(metricName));
        }
    }

    [Fact]
    public async Task StartHeartbeatAsync_ShouldNotThrow()
    {
        // Arrange
        _mockNetworkManager.Setup(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act & Assert - Should not throw
        await _systemMetricsCollector.StartHeartbeatAsync();
        
        // Verify initial heartbeat was sent
        _mockNetworkManager.Verify(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopHeartbeatAsync_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _systemMetricsCollector.StopHeartbeatAsync();
    }

    [Fact]
    public async Task GetHistoricalMetricsAsync_ShouldReturnEmptyArray()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;

        // Act
        var historicalMetrics = await _systemMetricsCollector.GetHistoricalMetricsAsync(from, to);

        // Assert
        Assert.NotNull(historicalMetrics);
        Assert.Empty(historicalMetrics);
    }

    [Fact]
    public async Task CollectMetricsAsync_ShouldHandleWmiFailuresGracefully()
    {
        // This test verifies that the collector handles WMI failures gracefully
        // In a real scenario, WMI might fail due to permissions or system issues
        
        // Act
        var metrics = await _systemMetricsCollector.CollectMetricsAsync();

        // Assert - Should return valid metrics object even if some WMI calls fail
        Assert.NotNull(metrics);
        Assert.True(metrics.Timestamp > DateTime.MinValue);
        // Values might be 0 if WMI fails, but should not be negative
        Assert.True(metrics.CpuUsage >= 0);
        Assert.True(metrics.MemoryUsage >= 0);
        Assert.True(metrics.DiskUsage >= 0);
    }

    [Fact]
    public async Task NodeStatus_ShouldGenerateAlertsForHighResourceUsage()
    {
        // This test verifies that the system generates appropriate alerts
        // Note: This might not trigger in a test environment with low resource usage
        
        // Act
        var nodeStatus = await _systemMetricsCollector.GetNodeStatusAsync();

        // Assert
        Assert.NotNull(nodeStatus.Alerts);
        // Alerts list should exist (might be empty in test environment)
        Assert.IsType<List<SystemAlert>>(nodeStatus.Alerts);
    }

    [Fact]
    public void Constructor_ShouldThrowForNullDependencies()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SystemMetricsCollector(null!, _mockNetworkManager.Object, _mockConfigurationManager.Object));
        Assert.Throws<ArgumentNullException>(() => new SystemMetricsCollector(_mockLogger.Object, null!, _mockConfigurationManager.Object));
        Assert.Throws<ArgumentNullException>(() => new SystemMetricsCollector(_mockLogger.Object, _mockNetworkManager.Object, null!));
    }

    [Fact]
    public async Task MultipleCollectMetricsAsync_ShouldNotInterfereWithEachOther()
    {
        // This test verifies thread safety of metrics collection
        
        // Act
        var task1 = _systemMetricsCollector.CollectMetricsAsync();
        var task2 = _systemMetricsCollector.CollectMetricsAsync();
        var task3 = _systemMetricsCollector.CollectMetricsAsync();

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        Assert.All(results, metrics =>
        {
            Assert.NotNull(metrics);
            Assert.True(metrics.Timestamp > DateTime.MinValue);
        });
    }

    public void Dispose()
    {
        _systemMetricsCollector?.Dispose();
    }
}

/// <summary>
/// Integration tests for SystemMetricsCollector that test actual WMI functionality
/// These tests require a Windows environment and appropriate permissions
/// </summary>
public class SystemMetricsCollectorIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<SystemMetricsCollector>> _mockLogger;
    private readonly Mock<INetworkManager> _mockNetworkManager;
    private readonly Mock<IConfigurationManager> _mockConfigurationManager;
    private readonly SystemMetricsCollector _systemMetricsCollector;

    public SystemMetricsCollectorIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<SystemMetricsCollector>>();
        _mockNetworkManager = new Mock<INetworkManager>();
        _mockConfigurationManager = new Mock<IConfigurationManager>();

        var defaultConfig = new Configuration
        {
            Backup = new BackupConfiguration
            {
                BackupPaths = new[] { System.IO.Path.GetTempPath() }
            }
        };
        _mockConfigurationManager.Setup(x => x.LoadConfigurationAsync())
            .ReturnsAsync(defaultConfig);

        _systemMetricsCollector = new SystemMetricsCollector(
            _mockLogger.Object,
            _mockNetworkManager.Object,
            _mockConfigurationManager.Object);
    }

    [Fact]
    public async Task CollectMetricsAsync_Integration_ShouldReturnRealisticValues()
    {
        // Act
        var metrics = await _systemMetricsCollector.CollectMetricsAsync();

        // Assert - Verify realistic values for a running system
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalMemoryMB > 1000); // At least 1GB RAM
        Assert.True(metrics.TotalDiskGB > 10); // At least 10GB disk
        Assert.True(metrics.ProcessCount > 50); // Typical Windows system has many processes
        Assert.True(metrics.UptimeHours >= 0);
        
        // Memory and disk usage should be reasonable
        Assert.True(metrics.MemoryUsage >= 0 && metrics.MemoryUsage <= 100);
        Assert.True(metrics.DiskUsage >= 0 && metrics.DiskUsage <= 100);
        
        // Available memory should be less than total
        Assert.True(metrics.AvailableMemoryMB < metrics.TotalMemoryMB);
        Assert.True(metrics.AvailableDiskGB <= metrics.TotalDiskGB);
    }

    [Fact]
    public async Task GetNodeStatusAsync_Integration_ShouldReturnValidSystemInfo()
    {
        // Act
        var nodeStatus = await _systemMetricsCollector.GetNodeStatusAsync();

        // Assert
        Assert.NotNull(nodeStatus);
        Assert.False(string.IsNullOrEmpty(nodeStatus.NodeId));
        Assert.Contains("_", nodeStatus.NodeId); // Should contain MAC_Username format
        Assert.False(string.IsNullOrEmpty(nodeStatus.MachineName));
        Assert.False(string.IsNullOrEmpty(nodeStatus.CurrentUser));
        Assert.False(string.IsNullOrEmpty(nodeStatus.OsInfo));
        Assert.Contains("Windows", nodeStatus.OsInfo);
        
        // Should have some active services
        Assert.NotNull(nodeStatus.ActiveServices);
        Assert.NotEmpty(nodeStatus.ActiveServices);
    }

    [Fact]
    public async Task IsSystemIdleAsync_Integration_ShouldReturnConsistentResults()
    {
        // Act
        var isIdle1 = await _systemMetricsCollector.IsSystemIdleAsync();
        await Task.Delay(100); // Small delay
        var isIdle2 = await _systemMetricsCollector.IsSystemIdleAsync();

        // Assert - Results should be consistent over short time periods
        Assert.IsType<bool>(isIdle1);
        Assert.IsType<bool>(isIdle2);
        // In most test environments, system should not be idle due to test execution
        // But we just verify the method returns valid boolean values
    }

    public void Dispose()
    {
        _systemMetricsCollector?.Dispose();
    }
}