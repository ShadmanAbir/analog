using System;
using System.Text.Json;
using EnterpriseITAgent.Models;
using Xunit;

namespace EnterpriseITAgent.Tests.Models;

public class EmailConfigurationTests
{
    [Fact]
    public void EmailConfiguration_ValidConfiguration_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var emailConfig = CreateValidEmailConfiguration();

        // Act
        var json = JsonSerializer.Serialize(emailConfig);
        var deserializedConfig = JsonSerializer.Deserialize<EmailConfiguration>(json);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(emailConfig.ImapServer, deserializedConfig.ImapServer);
        Assert.Equal(emailConfig.ImapPort, deserializedConfig.ImapPort);
        Assert.Equal(emailConfig.SmtpServer, deserializedConfig.SmtpServer);
        Assert.Equal(emailConfig.Username, deserializedConfig.Username);
        Assert.Equal(emailConfig.Password, deserializedConfig.Password);
    }

    [Fact]
    public void EmailConfiguration_ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var emailConfig = CreateValidEmailConfiguration();

        // Act
        var results = ConfigurationValidator.ValidateEmailConfiguration(emailConfig);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void EmailConfiguration_EmptyImapServer_ShouldFailValidation()
    {
        // Arrange
        var emailConfig = CreateValidEmailConfiguration();
        emailConfig.ImapServer = "";

        // Act
        var results = ConfigurationValidator.ValidateEmailConfiguration(emailConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("IMAP server"));
    }

    [Fact]
    public void EmailConfiguration_InvalidImapPort_ShouldFailValidation()
    {
        // Arrange
        var emailConfig = CreateValidEmailConfiguration();
        emailConfig.ImapPort = 0;

        // Act
        var results = ConfigurationValidator.ValidateEmailConfiguration(emailConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("IMAP port"));
    }

    [Fact]
    public void EmailConfiguration_EmptyUsername_ShouldFailValidation()
    {
        // Arrange
        var emailConfig = CreateValidEmailConfiguration();
        emailConfig.Username = "";

        // Act
        var results = ConfigurationValidator.ValidateEmailConfiguration(emailConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Username"));
    }

    [Fact]
    public void EmailConfiguration_InvalidMaxMailboxSize_ShouldFailValidation()
    {
        // Arrange
        var emailConfig = CreateValidEmailConfiguration();
        emailConfig.MaxMailboxSizeGB = 0;

        // Act
        var results = ConfigurationValidator.ValidateEmailConfiguration(emailConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Max mailbox size"));
    }

    [Fact]
    public void EmailConfiguration_JsonPropertyNames_ShouldMatchExpectedFormat()
    {
        // Arrange
        var emailConfig = CreateValidEmailConfiguration();

        // Act
        var json = JsonSerializer.Serialize(emailConfig);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        Assert.True(jsonDocument.RootElement.TryGetProperty("imapServer", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("imapPort", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("imapUseSsl", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("smtpServer", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("smtpPort", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("smtpUseSsl", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("username", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("password", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("maxMailboxSizeGB", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("enableArchiving", out _));
    }

    private static EmailConfiguration CreateValidEmailConfiguration()
    {
        return new EmailConfiguration
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
        };
    }
}