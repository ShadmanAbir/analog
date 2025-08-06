using System;
using System.Text.Json;
using EnterpriseITAgent.Models;
using Xunit;

namespace EnterpriseITAgent.Tests.Models;

public class SecurityConfigurationTests
{
    [Fact]
    public void SecurityConfiguration_ValidConfiguration_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var securityConfig = CreateValidSecurityConfiguration();

        // Act
        var json = JsonSerializer.Serialize(securityConfig);
        var deserializedConfig = JsonSerializer.Deserialize<SecurityConfiguration>(json);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(securityConfig.EnableEncryption, deserializedConfig.EnableEncryption);
        Assert.Equal(securityConfig.CertificatePath, deserializedConfig.CertificatePath);
        Assert.Equal(securityConfig.RequireAuthentication, deserializedConfig.RequireAuthentication);
        Assert.Equal(securityConfig.SessionTimeoutMinutes, deserializedConfig.SessionTimeoutMinutes);
        Assert.Equal(securityConfig.AllowedIpRanges.Length, deserializedConfig.AllowedIpRanges.Length);
        Assert.Equal(securityConfig.AllowedIpRanges[0], deserializedConfig.AllowedIpRanges[0]);
    }

    [Fact]
    public void SecurityConfiguration_ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var securityConfig = CreateValidSecurityConfiguration();

        // Act
        var results = ConfigurationValidator.ValidateSecurityConfiguration(securityConfig);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void SecurityConfiguration_InvalidSessionTimeout_ShouldFailValidation()
    {
        // Arrange
        var securityConfig = CreateValidSecurityConfiguration();
        securityConfig.SessionTimeoutMinutes = 0;

        // Act
        var results = ConfigurationValidator.ValidateSecurityConfiguration(securityConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Session timeout"));
    }

    [Fact]
    public void SecurityConfiguration_TooLongCertificatePath_ShouldFailValidation()
    {
        // Arrange
        var securityConfig = CreateValidSecurityConfiguration();
        securityConfig.CertificatePath = new string('a', 501); // Exceeds 500 character limit

        // Act
        var results = ConfigurationValidator.ValidateSecurityConfiguration(securityConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Certificate path"));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(1440)]
    public void SecurityConfiguration_ValidSessionTimeouts_ShouldPassValidation(int timeout)
    {
        // Arrange
        var securityConfig = CreateValidSecurityConfiguration();
        securityConfig.SessionTimeoutMinutes = timeout;

        // Act
        var results = ConfigurationValidator.ValidateSecurityConfiguration(securityConfig);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1441)]
    public void SecurityConfiguration_InvalidSessionTimeouts_ShouldFailValidation(int timeout)
    {
        // Arrange
        var securityConfig = CreateValidSecurityConfiguration();
        securityConfig.SessionTimeoutMinutes = timeout;

        // Act
        var results = ConfigurationValidator.ValidateSecurityConfiguration(securityConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Session timeout"));
    }

    [Fact]
    public void SecurityConfiguration_JsonPropertyNames_ShouldMatchExpectedFormat()
    {
        // Arrange
        var securityConfig = CreateValidSecurityConfiguration();

        // Act
        var json = JsonSerializer.Serialize(securityConfig);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        Assert.True(jsonDocument.RootElement.TryGetProperty("enableEncryption", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("certificatePath", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("requireAuthentication", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("sessionTimeoutMinutes", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("allowedIpRanges", out _));
    }

    private static SecurityConfiguration CreateValidSecurityConfiguration()
    {
        return new SecurityConfiguration
        {
            EnableEncryption = true,
            CertificatePath = "C:\\Certs\\certificate.pfx",
            RequireAuthentication = true,
            SessionTimeoutMinutes = 60,
            AllowedIpRanges = new[] { "192.168.1.0/24", "10.0.0.0/8", "172.16.0.0/12" }
        };
    }
}