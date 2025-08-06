using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EnterpriseITAgent.Models;
using Xunit;

namespace EnterpriseITAgent.Tests.Models;

public class ConfigurationTests
{
    [Fact]
    public void Configuration_ValidConfiguration_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserializedConfig = JsonSerializer.Deserialize<Configuration>(json);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(config.NodeId, deserializedConfig.NodeId);
        Assert.Equal(config.Version, deserializedConfig.Version);
        Assert.NotNull(deserializedConfig.Email);
        Assert.Equal(config.Email!.ImapServer, deserializedConfig.Email!.ImapServer);
        Assert.Equal(config.Email.Username, deserializedConfig.Email.Username);
    }

    [Fact]
    public void Configuration_ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var isValid = ConfigurationValidator.IsValid(config);
        var errors = ConfigurationValidator.GetValidationErrors(config);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Configuration_EmptyNodeId_ShouldFailValidation()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.NodeId = "";

        // Act
        var isValid = ConfigurationValidator.IsValid(config);
        var errors = ConfigurationValidator.GetValidationErrors(config);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("NodeId"));
    }

    [Fact]
    public void Configuration_InvalidVersion_ShouldFailValidation()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Version = "invalid-version";

        // Act
        var isValid = ConfigurationValidator.IsValid(config);
        var errors = ConfigurationValidator.GetValidationErrors(config);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Version"));
    }

    [Fact]
    public void Configuration_JsonPropertyNames_ShouldMatchExpectedFormat()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var json = JsonSerializer.Serialize(config);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        Assert.True(jsonDocument.RootElement.TryGetProperty("nodeId", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("email", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("printers", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("backup", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("security", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("version", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("lastUpdated", out _));
    }

    private static Configuration CreateValidConfiguration()
    {
        return new Configuration
        {
            NodeId = "test-node-001",
            Version = "1.0.0",
            LastUpdated = DateTime.UtcNow,
            Email = new EmailConfiguration
            {
                ImapServer = "imap.example.com",
                ImapPort = 993,
                ImapUseSsl = true,
                SmtpServer = "smtp.example.com",
                SmtpPort = 587,
                SmtpUseSsl = true,
                Username = "test@example.com",
                Password = "password123",
                MaxMailboxSizeGB = 5,
                EnableArchiving = true
            },
            Printers = new[]
            {
                new PrinterConfiguration
                {
                    Name = "Office Printer",
                    IpAddress = "192.168.1.100",
                    DriverPath = "C:\\Drivers\\printer.inf",
                    IsDefault = true,
                    Settings = new Dictionary<string, string>
                    {
                        { "ColorMode", "Color" },
                        { "PaperSize", "A4" }
                    }
                }
            },
            Backup = new BackupConfiguration
            {
                BackupPaths = new[] { "C:\\Documents", "C:\\Projects" },
                MaxBackupSizeGB = 100,
                RetentionDays = 30,
                EnableDistributedBackup = true,
                EncryptionKey = "ThisIsATestEncryptionKeyWith32Chars"
            },
            Security = new SecurityConfiguration
            {
                EnableEncryption = true,
                CertificatePath = "C:\\Certs\\certificate.pfx",
                RequireAuthentication = true,
                SessionTimeoutMinutes = 60,
                AllowedIpRanges = new[] { "192.168.1.0/24", "10.0.0.0/8" }
            },
            CustomSettings = new Dictionary<string, object>
            {
                { "Theme", "Dark" },
                { "AutoStart", true }
            }
        };
    }
}