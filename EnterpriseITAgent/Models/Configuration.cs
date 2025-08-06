using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EnterpriseITAgent.Models;

/// <summary>
/// Main configuration model for the Enterprise IT Agent
/// </summary>
public class Configuration
{
    /// <summary>
    /// Unique identifier for this node
    /// </summary>
    [JsonPropertyName("nodeId")]
    [Required(ErrorMessage = "NodeId is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "NodeId must be between 1 and 100 characters")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Email configuration settings
    /// </summary>
    [JsonPropertyName("email")]
    public EmailConfiguration? Email { get; set; }

    /// <summary>
    /// Printer configuration settings
    /// </summary>
    [JsonPropertyName("printers")]
    public PrinterConfiguration[]? Printers { get; set; }

    /// <summary>
    /// Backup configuration settings
    /// </summary>
    [JsonPropertyName("backup")]
    public BackupConfiguration? Backup { get; set; }

    /// <summary>
    /// Security configuration settings
    /// </summary>
    [JsonPropertyName("security")]
    public SecurityConfiguration? Security { get; set; }

    /// <summary>
    /// Custom application settings
    /// </summary>
    [JsonPropertyName("customSettings")]
    public Dictionary<string, object>? CustomSettings { get; set; }

    /// <summary>
    /// Configuration version for tracking updates
    /// </summary>
    [JsonPropertyName("version")]
    [Required(ErrorMessage = "Version is required")]
    [RegularExpression(@"^\d+\.\d+\.\d+$", ErrorMessage = "Version must be in format x.y.z")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Timestamp when configuration was last updated
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailConfiguration
{
    [JsonPropertyName("imapServer")]
    [Required(ErrorMessage = "IMAP server is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "IMAP server must be between 1 and 255 characters")]
    public string ImapServer { get; set; } = string.Empty;

    [JsonPropertyName("imapPort")]
    [Range(1, 65535, ErrorMessage = "IMAP port must be between 1 and 65535")]
    public int ImapPort { get; set; } = 993;

    [JsonPropertyName("imapUseSsl")]
    public bool ImapUseSsl { get; set; } = true;

    [JsonPropertyName("smtpServer")]
    [Required(ErrorMessage = "SMTP server is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "SMTP server must be between 1 and 255 characters")]
    public string SmtpServer { get; set; } = string.Empty;

    [JsonPropertyName("smtpPort")]
    [Range(1, 65535, ErrorMessage = "SMTP port must be between 1 and 65535")]
    public int SmtpPort { get; set; } = 587;

    [JsonPropertyName("smtpUseSsl")]
    public bool SmtpUseSsl { get; set; } = true;

    [JsonPropertyName("username")]
    [Required(ErrorMessage = "Username is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 255 characters")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "Password is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 255 characters")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("maxMailboxSizeGB")]
    [Range(1, 1000, ErrorMessage = "Max mailbox size must be between 1 and 1000 GB")]
    public int MaxMailboxSizeGB { get; set; } = 5;

    [JsonPropertyName("enableArchiving")]
    public bool EnableArchiving { get; set; } = true;
}

/// <summary>
/// Printer configuration settings
/// </summary>
public class PrinterConfiguration
{
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Printer name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Printer name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    [Required(ErrorMessage = "IP address is required")]
    [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", 
        ErrorMessage = "Invalid IP address format")]
    public string IpAddress { get; set; } = string.Empty;

    [JsonPropertyName("driverPath")]
    [StringLength(500, ErrorMessage = "Driver path cannot exceed 500 characters")]
    public string DriverPath { get; set; } = string.Empty;

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("settings")]
    public Dictionary<string, string>? Settings { get; set; }
}

/// <summary>
/// Backup configuration settings
/// </summary>
public class BackupConfiguration
{
    [JsonPropertyName("backupPaths")]
    [Required(ErrorMessage = "Backup paths are required")]
    [MinLength(1, ErrorMessage = "At least one backup path must be specified")]
    public string[] BackupPaths { get; set; } = Array.Empty<string>();

    [JsonPropertyName("maxBackupSizeGB")]
    [Range(1, 10000, ErrorMessage = "Max backup size must be between 1 and 10000 GB")]
    public int MaxBackupSizeGB { get; set; } = 100;

    [JsonPropertyName("retentionDays")]
    [Range(1, 3650, ErrorMessage = "Retention days must be between 1 and 3650 days")]
    public int RetentionDays { get; set; } = 30;

    [JsonPropertyName("enableDistributedBackup")]
    public bool EnableDistributedBackup { get; set; } = true;

    [JsonPropertyName("encryptionKey")]
    [StringLength(256, MinimumLength = 32, ErrorMessage = "Encryption key must be between 32 and 256 characters")]
    public string EncryptionKey { get; set; } = string.Empty;
}

/// <summary>
/// Security configuration settings
/// </summary>
public class SecurityConfiguration
{
    [JsonPropertyName("enableEncryption")]
    public bool EnableEncryption { get; set; } = true;

    [JsonPropertyName("certificatePath")]
    [StringLength(500, ErrorMessage = "Certificate path cannot exceed 500 characters")]
    public string CertificatePath { get; set; } = string.Empty;

    [JsonPropertyName("requireAuthentication")]
    public bool RequireAuthentication { get; set; } = true;

    [JsonPropertyName("sessionTimeoutMinutes")]
    [Range(5, 1440, ErrorMessage = "Session timeout must be between 5 and 1440 minutes")]
    public int SessionTimeoutMinutes { get; set; } = 60;

    [JsonPropertyName("allowedIpRanges")]
    public string[] AllowedIpRanges { get; set; } = Array.Empty<string>();
}