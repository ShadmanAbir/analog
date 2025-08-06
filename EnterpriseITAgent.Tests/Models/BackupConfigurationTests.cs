using System;
using System.Text.Json;
using EnterpriseITAgent.Models;
using Xunit;

namespace EnterpriseITAgent.Tests.Models;

public class BackupConfigurationTests
{
    [Fact]
    public void BackupConfiguration_ValidConfiguration_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();

        // Act
        var json = JsonSerializer.Serialize(backupConfig);
        var deserializedConfig = JsonSerializer.Deserialize<BackupConfiguration>(json);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(backupConfig.BackupPaths.Length, deserializedConfig.BackupPaths.Length);
        Assert.Equal(backupConfig.BackupPaths[0], deserializedConfig.BackupPaths[0]);
        Assert.Equal(backupConfig.MaxBackupSizeGB, deserializedConfig.MaxBackupSizeGB);
        Assert.Equal(backupConfig.RetentionDays, deserializedConfig.RetentionDays);
        Assert.Equal(backupConfig.EnableDistributedBackup, deserializedConfig.EnableDistributedBackup);
        Assert.Equal(backupConfig.EncryptionKey, deserializedConfig.EncryptionKey);
    }

    [Fact]
    public void BackupConfiguration_ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();

        // Act
        var results = ConfigurationValidator.ValidateBackupConfiguration(backupConfig);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void BackupConfiguration_EmptyBackupPaths_ShouldFailValidation()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();
        backupConfig.BackupPaths = Array.Empty<string>();

        // Act
        var results = ConfigurationValidator.ValidateBackupConfiguration(backupConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("At least one backup path"));
    }

    [Fact]
    public void BackupConfiguration_InvalidMaxBackupSize_ShouldFailValidation()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();
        backupConfig.MaxBackupSizeGB = 0;

        // Act
        var results = ConfigurationValidator.ValidateBackupConfiguration(backupConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Max backup size"));
    }

    [Fact]
    public void BackupConfiguration_InvalidRetentionDays_ShouldFailValidation()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();
        backupConfig.RetentionDays = 0;

        // Act
        var results = ConfigurationValidator.ValidateBackupConfiguration(backupConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Retention days"));
    }

    [Fact]
    public void BackupConfiguration_ShortEncryptionKey_ShouldFailValidation()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();
        backupConfig.EncryptionKey = "short";

        // Act
        var results = ConfigurationValidator.ValidateBackupConfiguration(backupConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Encryption key"));
    }

    [Fact]
    public void BackupConfiguration_EmptyPathInArray_ShouldFailValidation()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();
        backupConfig.BackupPaths = new[] { "C:\\Documents", "", "C:\\Projects" };

        // Act
        var results = ConfigurationValidator.ValidateBackupConfiguration(backupConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Backup path at index 1 cannot be empty"));
    }

    [Fact]
    public void BackupConfiguration_JsonPropertyNames_ShouldMatchExpectedFormat()
    {
        // Arrange
        var backupConfig = CreateValidBackupConfiguration();

        // Act
        var json = JsonSerializer.Serialize(backupConfig);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        Assert.True(jsonDocument.RootElement.TryGetProperty("backupPaths", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("maxBackupSizeGB", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("retentionDays", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("enableDistributedBackup", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("encryptionKey", out _));
    }

    private static BackupConfiguration CreateValidBackupConfiguration()
    {
        return new BackupConfiguration
        {
            BackupPaths = new[] { "C:\\Documents", "C:\\Projects", "C:\\Data" },
            MaxBackupSizeGB = 100,
            RetentionDays = 30,
            EnableDistributedBackup = true,
            EncryptionKey = "ThisIsATestEncryptionKeyWith32Chars"
        };
    }
}