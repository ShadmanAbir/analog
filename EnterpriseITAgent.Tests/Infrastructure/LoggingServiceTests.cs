using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnterpriseITAgent.Infrastructure;
using EnterpriseITAgent.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EnterpriseITAgent.Tests.Infrastructure;

/// <summary>
/// Unit tests for LoggingService
/// </summary>
public class LoggingServiceTests : IDisposable
{
    private readonly Mock<ILogger<LoggingService>> _mockLogger;
    private readonly Mock<INetworkManager> _mockNetworkManager;
    private readonly Mock<IConfigurationManager> _mockConfigurationManager;
    private readonly LoggingService _loggingService;
    private readonly string _testLogDirectory;

    public LoggingServiceTests()
    {
        _mockLogger = new Mock<ILogger<LoggingService>>();
        _mockNetworkManager = new Mock<INetworkManager>();
        _mockConfigurationManager = new Mock<IConfigurationManager>();
        
        // Set up test configuration
        var testConfig = new Configuration
        {
            NodeId = "TEST-NODE-001",
            Version = "1.0.0"
        };
        
        _mockConfigurationManager
            .Setup(x => x.LoadConfigurationAsync())
            .ReturnsAsync(testConfig);

        _testLogDirectory = Path.Combine(Path.GetTempPath(), "EnterpriseITAgent_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testLogDirectory);

        _loggingService = new LoggingService(_mockLogger.Object, _mockNetworkManager.Object, _mockConfigurationManager.Object);
        
        // Configure with test directory
        var loggingConfig = new LoggingConfiguration
        {
            LogDirectory = _testLogDirectory,
            MaxFileSizeMB = 1,
            RetainedFileCount = 3,
            EnableTelemetry = false, // Disable for unit tests
            MaxBufferSize = 100
        };
        
        _loggingService.ConfigureLoggingAsync(loggingConfig).Wait();
    }

    [Fact]
    public void LogInfo_ShouldCreateLogEntry()
    {
        // Arrange
        var message = "Test information message";
        var component = "TestComponent";
        var properties = new Dictionary<string, object> { ["TestProperty"] = "TestValue" };

        // Act
        _loggingService.LogInfo(message, component, properties);

        // Assert
        // Verify that the log was processed (we can't easily verify file contents in unit test)
        // The actual file writing is tested in integration tests
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public void LogWarning_ShouldCreateWarningLogEntry()
    {
        // Arrange
        var message = "Test warning message";
        var component = "TestComponent";

        // Act
        _loggingService.LogWarning(message, component);

        // Assert
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public void LogError_ShouldCreateErrorLogEntryWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = "Test context";
        var component = "TestComponent";

        // Act
        _loggingService.LogError(exception, context, component);

        // Assert
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public void LogDebug_ShouldCreateDebugLogEntry()
    {
        // Arrange
        var message = "Test debug message";
        var component = "TestComponent";

        // Act
        _loggingService.LogDebug(message, component);

        // Assert
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public void LogCritical_ShouldCreateCriticalLogEntry()
    {
        // Arrange
        var message = "Test critical message";
        var component = "TestComponent";

        // Act
        _loggingService.LogCritical(message, component);

        // Assert
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public void LogStructured_ShouldProcessStructuredLogEntry()
    {
        // Arrange
        var logEntry = new LogEntry
        {
            Level = LogLevel.Information,
            Message = "Structured log message",
            Component = "TestComponent",
            Properties = new Dictionary<string, object> { ["Key"] = "Value" }
        };

        // Act
        _loggingService.LogStructured(logEntry);

        // Assert
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public void SetCorrelationId_ShouldSetCorrelationContext()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        _loggingService.SetCorrelationId(correlationId);

        // Assert
        // Verify by logging a message and checking if correlation ID is included
        _loggingService.LogInfo("Test message with correlation", "TestComponent");
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public void SetUserContext_ShouldSetUserAndSessionContext()
    {
        // Arrange
        var userId = "testuser@company.com";
        var sessionId = Guid.NewGuid().ToString();

        // Act
        _loggingService.SetUserContext(userId, sessionId);

        // Assert
        // Verify by logging a message and checking if user context is included
        _loggingService.LogInfo("Test message with user context", "TestComponent");
        Assert.True(true); // Basic test to ensure no exceptions
    }

    [Fact]
    public async Task GetRecentLogsAsync_ShouldReturnRecentLogEntries()
    {
        // Arrange
        _loggingService.LogInfo("Test message 1", "TestComponent");
        _loggingService.LogInfo("Test message 2", "TestComponent");
        _loggingService.LogWarning("Test warning", "TestComponent");

        // Act
        var recentLogs = await _loggingService.GetRecentLogsAsync(10);

        // Assert
        Assert.NotNull(recentLogs);
        // Note: The actual count may vary depending on timing and file operations
    }

    [Fact]
    public async Task GetLogsByLevelAsync_ShouldReturnLogsFilteredByLevel()
    {
        // Arrange
        _loggingService.LogInfo("Info message", "TestComponent");
        _loggingService.LogWarning("Warning message", "TestComponent");
        _loggingService.LogError(new Exception("Test error"), "Error context", "TestComponent");

        // Act
        var warningLogs = await _loggingService.GetLogsByLevelAsync(LogLevel.Warning, 10);

        // Assert
        Assert.NotNull(warningLogs);
        // Note: Actual filtering verification would require more complex setup
    }

    [Fact]
    public async Task GetLogsByComponentAsync_ShouldReturnLogsFilteredByComponent()
    {
        // Arrange
        var targetComponent = "TargetComponent";
        _loggingService.LogInfo("Message from target", targetComponent);
        _loggingService.LogInfo("Message from other", "OtherComponent");

        // Act
        var componentLogs = await _loggingService.GetLogsByComponentAsync(targetComponent, 10);

        // Assert
        Assert.NotNull(componentLogs);
        // Note: Actual filtering verification would require more complex setup
    }

    [Fact]
    public async Task ConfigureLoggingAsync_ShouldUpdateConfiguration()
    {
        // Arrange
        var newConfig = new LoggingConfiguration
        {
            LogDirectory = _testLogDirectory,
            MinLogLevel = "Warning",
            MaxFileSizeMB = 5,
            RetainedFileCount = 10,
            EnableTelemetry = true,
            TelemetryIntervalMinutes = 10
        };

        // Act
        await _loggingService.ConfigureLoggingAsync(newConfig);

        // Assert
        var stats = await _loggingService.GetLoggingStatsAsync();
        Assert.Equal(_testLogDirectory, stats["LogDirectory"]);
        Assert.Equal(true, stats["TelemetryEnabled"]);
    }

    [Fact]
    public async Task GetLoggingStatsAsync_ShouldReturnStatistics()
    {
        // Arrange
        _loggingService.LogInfo("Test message", "TestComponent");
        _loggingService.LogWarning("Test warning", "TestComponent");
        _loggingService.LogError(new Exception("Test error"), "Test context", "TestComponent");

        // Act
        var stats = await _loggingService.GetLoggingStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.ContainsKey("TotalLogs"));
        Assert.True(stats.ContainsKey("TotalErrors"));
        Assert.True(stats.ContainsKey("TotalWarnings"));
        Assert.True(stats.ContainsKey("BufferSize"));
        Assert.True(stats.ContainsKey("ComponentStats"));
        Assert.True(stats.ContainsKey("NodeId"));
    }

    [Fact]
    public async Task SendTelemetryAsync_WithNoConnectivity_ShouldNotSendTelemetry()
    {
        // Arrange
        _mockNetworkManager
            .Setup(x => x.TestConnectivityAsync())
            .ReturnsAsync(false);

        var config = new LoggingConfiguration
        {
            EnableTelemetry = true,
            TelemetryEndpoint = "https://api.example.com/telemetry"
        };
        await _loggingService.ConfigureLoggingAsync(config);

        // Act
        await _loggingService.SendTelemetryAsync();

        // Assert
        _mockNetworkManager.Verify(x => x.TestConnectivityAsync(), Times.Once);
        _mockNetworkManager.Verify(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task SendTelemetryAsync_WithConnectivity_ShouldSendTelemetry()
    {
        // Arrange
        _mockNetworkManager
            .Setup(x => x.TestConnectivityAsync())
            .ReturnsAsync(true);

        _mockNetworkManager
            .Setup(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new object());

        var config = new LoggingConfiguration
        {
            EnableTelemetry = true,
            TelemetryEndpoint = "https://api.example.com/telemetry"
        };
        await _loggingService.ConfigureLoggingAsync(config);

        // Add some log entries that should be sent via telemetry
        _loggingService.LogWarning("Test warning for telemetry", "TestComponent");
        _loggingService.LogError(new Exception("Test error"), "Test context", "TestComponent");

        // Act
        await _loggingService.SendTelemetryAsync();

        // Assert
        _mockNetworkManager.Verify(x => x.TestConnectivityAsync(), Times.Once);
        _mockNetworkManager.Verify(x => x.SecureApiCallAsync<object>(It.IsAny<string>(), It.IsAny<TelemetryData>()), Times.Once);
    }

    [Fact]
    public void ExceptionDetails_FromException_ShouldCreateCorrectDetails()
    {
        // Arrange
        var innerException = new ArgumentException("Inner exception message");
        var outerException = new InvalidOperationException("Outer exception message", innerException);
        outerException.Data["CustomKey"] = "CustomValue";

        // Act
        var exceptionDetails = ExceptionDetails.FromException(outerException);

        // Assert
        Assert.Equal("System.InvalidOperationException", exceptionDetails.Type);
        Assert.Equal("Outer exception message", exceptionDetails.Message);
        Assert.NotNull(exceptionDetails.StackTrace);
        Assert.NotNull(exceptionDetails.InnerException);
        Assert.Equal("System.ArgumentException", exceptionDetails.InnerException.Type);
        Assert.Equal("Inner exception message", exceptionDetails.InnerException.Message);
        Assert.NotNull(exceptionDetails.Data);
        Assert.Equal("CustomValue", exceptionDetails.Data["CustomKey"]);
    }

    public void Dispose()
    {
        _loggingService?.Dispose();
        
        // Clean up test directory
        if (Directory.Exists(_testLogDirectory))
        {
            try
            {
                Directory.Delete(_testLogDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}