using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EnterpriseITAgent.Models;

/// <summary>
/// Email message entity for database storage
/// </summary>
[Table("EmailMessages")]
public class EmailMessage
{
    /// <summary>
    /// Unique identifier for the email message
    /// </summary>
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Thread identifier for conversation grouping
    /// </summary>
    [JsonPropertyName("threadId")]
    [StringLength(100)]
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// Email subject
    /// </summary>
    [JsonPropertyName("subject")]
    [StringLength(500)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body content
    /// </summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address
    /// </summary>
    [JsonPropertyName("from")]
    [StringLength(255)]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Recipient email addresses (JSON array)
    /// </summary>
    [JsonPropertyName("to")]
    [StringLength(1000)]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// CC email addresses (JSON array)
    /// </summary>
    [JsonPropertyName("cc")]
    [StringLength(1000)]
    public string Cc { get; set; } = string.Empty;

    /// <summary>
    /// BCC email addresses (JSON array)
    /// </summary>
    [JsonPropertyName("bcc")]
    [StringLength(1000)]
    public string Bcc { get; set; } = string.Empty;

    /// <summary>
    /// Email timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the email has been read
    /// </summary>
    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    /// <summary>
    /// Whether the email is flagged
    /// </summary>
    [JsonPropertyName("isFlagged")]
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Email folder/mailbox name
    /// </summary>
    [JsonPropertyName("folder")]
    [StringLength(100)]
    public string Folder { get; set; } = "INBOX";

    /// <summary>
    /// IMAP UID for synchronization
    /// </summary>
    [JsonPropertyName("imapUid")]
    public uint ImapUid { get; set; }

    /// <summary>
    /// Message size in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Whether the email is archived
    /// </summary>
    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }

    /// <summary>
    /// Navigation property for attachments
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
}

/// <summary>
/// Email attachment entity for database storage
/// </summary>
[Table("EmailAttachments")]
public class EmailAttachment
{
    /// <summary>
    /// Unique identifier for the attachment
    /// </summary>
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Foreign key to the email message
    /// </summary>
    [JsonPropertyName("emailId")]
    [StringLength(100)]
    public string EmailId { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    [JsonPropertyName("fileName")]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content hash for deduplication
    /// </summary>
    [JsonPropertyName("contentHash")]
    [StringLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// MIME content type
    /// </summary>
    [JsonPropertyName("contentType")]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Storage path on disk
    /// </summary>
    [JsonPropertyName("storagePath")]
    [StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the attachment is encrypted
    /// </summary>
    [JsonPropertyName("isEncrypted")]
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Reference count for deduplication
    /// </summary>
    [JsonPropertyName("referenceCount")]
    public int ReferenceCount { get; set; } = 1;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the email message
    /// </summary>
    [JsonIgnore]
    [ForeignKey("EmailId")]
    public virtual EmailMessage? Email { get; set; }
}