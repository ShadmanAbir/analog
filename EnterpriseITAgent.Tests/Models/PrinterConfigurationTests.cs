using System;
using System.Collections.Generic;
using System.Text.Json;
using EnterpriseITAgent.Models;
using Xunit;

namespace EnterpriseITAgent.Tests.Models;

public class PrinterConfigurationTests
{
    [Fact]
    public void PrinterConfiguration_ValidConfiguration_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var printerConfig = CreateValidPrinterConfiguration();

        // Act
        var json = JsonSerializer.Serialize(printerConfig);
        var deserializedConfig = JsonSerializer.Deserialize<PrinterConfiguration>(json);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(printerConfig.Name, deserializedConfig.Name);
        Assert.Equal(printerConfig.IpAddress, deserializedConfig.IpAddress);
        Assert.Equal(printerConfig.DriverPath, deserializedConfig.DriverPath);
        Assert.Equal(printerConfig.IsDefault, deserializedConfig.IsDefault);
        Assert.NotNull(deserializedConfig.Settings);
        Assert.Equal(printerConfig.Settings!["ColorMode"], deserializedConfig.Settings!["ColorMode"]);
    }

    [Fact]
    public void PrinterConfiguration_ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var printerConfig = CreateValidPrinterConfiguration();

        // Act
        var results = ConfigurationValidator.ValidatePrinterConfiguration(printerConfig);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void PrinterConfiguration_EmptyName_ShouldFailValidation()
    {
        // Arrange
        var printerConfig = CreateValidPrinterConfiguration();
        printerConfig.Name = "";

        // Act
        var results = ConfigurationValidator.ValidatePrinterConfiguration(printerConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Printer name"));
    }

    [Fact]
    public void PrinterConfiguration_InvalidIpAddress_ShouldFailValidation()
    {
        // Arrange
        var printerConfig = CreateValidPrinterConfiguration();
        printerConfig.IpAddress = "invalid-ip";

        // Act
        var results = ConfigurationValidator.ValidatePrinterConfiguration(printerConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Invalid IP address"));
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("255.255.255.255")]
    public void PrinterConfiguration_ValidIpAddresses_ShouldPassValidation(string ipAddress)
    {
        // Arrange
        var printerConfig = CreateValidPrinterConfiguration();
        printerConfig.IpAddress = ipAddress;

        // Act
        var results = ConfigurationValidator.ValidatePrinterConfiguration(printerConfig);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("256.1.1.1")]
    [InlineData("192.168.1")]
    [InlineData("192.168.1.1.1")]
    [InlineData("abc.def.ghi.jkl")]
    public void PrinterConfiguration_InvalidIpAddresses_ShouldFailValidation(string ipAddress)
    {
        // Arrange
        var printerConfig = CreateValidPrinterConfiguration();
        printerConfig.IpAddress = ipAddress;

        // Act
        var results = ConfigurationValidator.ValidatePrinterConfiguration(printerConfig);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Invalid IP address"));
    }

    [Fact]
    public void PrinterConfiguration_JsonPropertyNames_ShouldMatchExpectedFormat()
    {
        // Arrange
        var printerConfig = CreateValidPrinterConfiguration();

        // Act
        var json = JsonSerializer.Serialize(printerConfig);
        var jsonDocument = JsonDocument.Parse(json);

        // Assert
        Assert.True(jsonDocument.RootElement.TryGetProperty("name", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("ipAddress", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("driverPath", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("isDefault", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("settings", out _));
    }

    private static PrinterConfiguration CreateValidPrinterConfiguration()
    {
        return new PrinterConfiguration
        {
            Name = "Office Printer",
            IpAddress = "192.168.1.100",
            DriverPath = "C:\\Drivers\\printer.inf",
            IsDefault = true,
            Settings = new Dictionary<string, string>
            {
                { "ColorMode", "Color" },
                { "PaperSize", "A4" },
                { "Quality", "High" }
            }
        };
    }
}