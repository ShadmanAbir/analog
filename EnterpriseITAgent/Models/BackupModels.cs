using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EnterpriseITAgent.Models;

/// <summary>
/// Backup chunk entity for distributed storage
/// </summary>
[Table("BackupChunks")]
public class BackupChunk
{
    /// <summary>
    /// Unique identifier for the chunk
    /// </summary>
    [Key]
    [JsonPropertyName("chunkId")]
    public string ChunkId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Hash of the original file this chunk belongs to
    /// </summary>
    [JsonPropertyName("fileHash")]
    [StringLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash of this specific chunk
    /// </summary>
    [JsonPropertyName("chunkHash")]
    [StringLength(64)]
    public string ChunkHash { get; set; } = string.Empty;

    /// <summary>
    /// Chunk sequence number within the file
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Size of the chunk in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Storage path for the encrypted chunk data
    /// </summary>
    [JsonPropertyName("storagePath")]
    [StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the chunk is encrypted
    /// </summary>
    [JsonPropertyName("isEncrypted")]
    public bool IsEncrypted { get; set; } = true;

    /// <summary>
    /// Nodes that have a copy of this chunk (JSON array)
    /// </summary>
    [JsonPropertyName("replicatedNodes")]
    [StringLength(1000)]
    public string ReplicatedNodes { get; set; } = string.Empty;

    /// <summary>
    /// Target replication count
    /// </summary>
    [JsonPropertyName("targetReplicas")]
    public int TargetReplicas { get; set; } = 3;

    /// <summary>
    /// Current replication count
    /// </summary>
    [JsonPropertyName("currentReplicas")]
    public int CurrentReplicas { get; set; } = 1;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last verification timestamp
    /// </summary>
    [JsonPropertyName("lastVerified")]
    public DateTime LastVerified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to file version
    /// </summary>
    [JsonIgnore]
    [ForeignKey("FileHash")]
    public virtual FileVersion? FileVersion { get; set; }
}

/// <summary>
/// File version entity for backup versioning
/// </summary>
[Table("FileVersions")]
public class FileVersion
{
    /// <summary>
    /// Unique identifier for the file version
    /// </summary>
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Hash of the file content
    /// </summary>
    [JsonPropertyName("fileHash")]
    [StringLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Original file path
    /// </summary>
    [JsonPropertyName("filePath")]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File name
    /// </summary>
    [JsonPropertyName("fileName")]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Version identifier
    /// </summary>
    [JsonPropertyName("version")]
    [StringLength(50)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Chunk IDs that make up this file (JSON array)
    /// </summary>
    [JsonPropertyName("chunkIds")]
    [StringLength(2000)]
    public string ChunkIds { get; set; } = string.Empty;

    /// <summary>
    /// Total number of chunks
    /// </summary>
    [JsonPropertyName("chunkCount")]
    public int ChunkCount { get; set; }

    /// <summary>
    /// File creation timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp of original file
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this version is the current/latest
    /// </summary>
    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; } = true;

    /// <summary>
    /// Node that created this backup
    /// </summary>
    [JsonPropertyName("sourceNodeId")]
    [StringLength(100)]
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Backup job ID this version belongs to
    /// </summary>
    [JsonPropertyName("backupJobId")]
    [StringLength(100)]
    public string BackupJobId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for chunks
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<BackupChunk> Chunks { get; set; } = new List<BackupChunk>();
}

/// <summary>
/// Backup job entity for tracking backup operations
/// </summary>
[Table("BackupJobs")]
public class BackupJob
{
    /// <summary>
    /// Unique identifier for the backup job
    /// </summary>
    [Key]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Node that initiated the backup
    /// </summary>
    [JsonPropertyName("nodeId")]
    [StringLength(100)]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Backup job name/description
    /// </summary>
    [JsonPropertyName("name")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Source paths being backed up (JSON array)
    /// </summary>
    [JsonPropertyName("sourcePaths")]
    [StringLength(2000)]
    public string SourcePaths { get; set; } = string.Empty;

    /// <summary>
    /// Backup job status
    /// </summary>
    [JsonPropertyName("status")]
    public BackupJobStatus Status { get; set; } = BackupJobStatus.Pending;

    /// <summary>
    /// Job start timestamp
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Job completion timestamp
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total files processed
    /// </summary>
    [JsonPropertyName("totalFiles")]
    public int TotalFiles { get; set; }

    /// <summary>
    /// Files successfully backed up
    /// </summary>
    [JsonPropertyName("successfulFiles")]
    public int SuccessfulFiles { get; set; }

    /// <summary>
    /// Files that failed to backup
    /// </summary>
    [JsonPropertyName("failedFiles")]
    public int FailedFiles { get; set; }

    /// <summary>
    /// Total bytes processed
    /// </summary>
    [JsonPropertyName("totalBytes")]
    public long TotalBytes { get; set; }

    /// <summary>
    /// Error message if job failed
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    [JsonPropertyName("progress")]
    public double Progress { get; set; }
}

/// <summary>
/// Backup job status enumeration
/// </summary>
public enum BackupJobStatus
{
    /// <summary>
    /// Job is pending execution
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed with errors
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Job is paused
    /// </summary>
    Paused = 5
}