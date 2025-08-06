using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EnterpriseITAgent.Models;

namespace EnterpriseITAgent.Infrastructure;

/// <summary>
/// Entity Framework DbContext for the Enterprise IT Agent
/// </summary>
public class EnterpriseDbContext : DbContext
{
    private readonly ILogger<EnterpriseDbContext>? _logger;
    private readonly string _connectionString;

    /// <summary>
    /// Email messages table
    /// </summary>
    public DbSet<EmailMessage> EmailMessages { get; set; } = null!;

    /// <summary>
    /// Email attachments table
    /// </summary>
    public DbSet<EmailAttachment> EmailAttachments { get; set; } = null!;

    /// <summary>
    /// Backup chunks table
    /// </summary>
    public DbSet<BackupChunk> BackupChunks { get; set; } = null!;

    /// <summary>
    /// File versions table
    /// </summary>
    public DbSet<FileVersion> FileVersions { get; set; } = null!;

    /// <summary>
    /// Backup jobs table
    /// </summary>
    public DbSet<BackupJob> BackupJobs { get; set; } = null!;

    /// <summary>
    /// Log entries table
    /// </summary>
    public DbSet<LogEntry> LogEntries { get; set; } = null!;



    /// <summary>
    /// Node status table
    /// </summary>
    public DbSet<NodeStatus> NodeStatus { get; set; } = null!;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    public EnterpriseDbContext(DbContextOptions<EnterpriseDbContext> options, ILogger<EnterpriseDbContext>? logger = null)
        : base(options)
    {
        _logger = logger;
        _connectionString = Database.GetConnectionString() ?? string.Empty;
    }

    /// <summary>
    /// Constructor with connection string
    /// </summary>
    public EnterpriseDbContext(string connectionString, ILogger<EnterpriseDbContext>? logger = null)
        : base()
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    /// <summary>
    /// Default constructor for migrations
    /// </summary>
    public EnterpriseDbContext() : base()
    {
        _connectionString = GetDefaultConnectionString();
    }

    /// <summary>
    /// Configure the database connection
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = !string.IsNullOrEmpty(_connectionString) 
                ? _connectionString 
                : GetDefaultConnectionString();

            optionsBuilder.UseSqlite(connectionString, options =>
            {
                options.CommandTimeout(30);
            });

            // Enable sensitive data logging in development
            if (_logger != null)
            {
                optionsBuilder.LogTo(message => _logger.LogDebug(message))
                             .EnableSensitiveDataLogging()
                             .EnableDetailedErrors();
            }
        }
    }

    /// <summary>
    /// Configure entity relationships and constraints
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure EmailMessage entity
        modelBuilder.Entity<EmailMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ThreadId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Folder);
            entity.HasIndex(e => e.ImapUid);
            entity.HasIndex(e => new { e.Folder, e.ImapUid }).IsUnique();
            
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.ThreadId).HasMaxLength(100);
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.From).HasMaxLength(255);
            entity.Property(e => e.To).HasMaxLength(1000);
            entity.Property(e => e.Cc).HasMaxLength(1000);
            entity.Property(e => e.Bcc).HasMaxLength(1000);
            entity.Property(e => e.Folder).HasMaxLength(100);
        });

        // Configure EmailAttachment entity
        modelBuilder.Entity<EmailAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.EmailId);
            
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.EmailId).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.StoragePath).HasMaxLength(500);

            entity.HasOne(e => e.Email)
                  .WithMany(e => e.Attachments)
                  .HasForeignKey(e => e.EmailId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BackupChunk entity
        modelBuilder.Entity<BackupChunk>(entity =>
        {
            entity.HasKey(e => e.ChunkId);
            entity.HasIndex(e => e.FileHash);
            entity.HasIndex(e => e.ChunkHash).IsUnique();
            entity.HasIndex(e => new { e.FileHash, e.SequenceNumber }).IsUnique();
            
            entity.Property(e => e.ChunkId).HasMaxLength(100);
            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.ChunkHash).HasMaxLength(64);
            entity.Property(e => e.StoragePath).HasMaxLength(500);
            entity.Property(e => e.ReplicatedNodes).HasMaxLength(1000);
        });

        // Configure FileVersion entity
        modelBuilder.Entity<FileVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileHash).IsUnique();
            entity.HasIndex(e => e.FilePath);
            entity.HasIndex(e => e.BackupJobId);
            entity.HasIndex(e => e.SourceNodeId);
            
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.Version).HasMaxLength(50);
            entity.Property(e => e.ChunkIds).HasMaxLength(2000);
            entity.Property(e => e.SourceNodeId).HasMaxLength(100);
            entity.Property(e => e.BackupJobId).HasMaxLength(100);

            entity.HasMany(e => e.Chunks)
                  .WithOne(e => e.FileVersion)
                  .HasForeignKey(e => e.FileHash)
                  .HasPrincipalKey(e => e.FileHash)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BackupJob entity
        modelBuilder.Entity<BackupJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NodeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
            
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.NodeId).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SourcePaths).HasMaxLength(2000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
        });

        // Configure LogEntry entity
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.Component);
            entity.HasIndex(e => e.NodeId);
            
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Component).HasMaxLength(100);
            entity.Property(e => e.NodeId).HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.SessionId).HasMaxLength(100);

            // Configure complex properties as JSON
            entity.Property(e => e.Exception)
                  .HasConversion(
                      v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<ExceptionDetails>(v, (System.Text.Json.JsonSerializerOptions?)null));

            entity.Property(e => e.Properties)
                  .HasConversion(
                      v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null));
        });

        // Configure NodeStatus entity
        modelBuilder.Entity<NodeStatus>(entity =>
        {
            entity.HasKey(e => e.NodeId);
            entity.HasIndex(e => e.LastHeartbeat);
            
            entity.Property(e => e.NodeId).HasMaxLength(100);
            entity.Property(e => e.Version).HasMaxLength(50);
            entity.Property(e => e.OsInfo).HasMaxLength(200);
            entity.Property(e => e.MachineName).HasMaxLength(100);
            entity.Property(e => e.CurrentUser).HasMaxLength(100);

            entity.Property(e => e.ActiveServices)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(e => e.Alerts)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<SystemAlert>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<SystemAlert>());

            // Configure the Metrics property as owned entity
            entity.OwnsOne(e => e.Metrics, metrics =>
            {
                metrics.Property(m => m.CustomMetrics)
                       .HasConversion(
                           v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                           v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (System.Text.Json.JsonSerializerOptions?)null));
            });
        });
    }

    /// <summary>
    /// Get the default connection string
    /// </summary>
    private static string GetDefaultConnectionString()
    {
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterpriseITAgent");
        Directory.CreateDirectory(dataDirectory);
        var dbPath = Path.Combine(dataDirectory, "enterprise.db");
        return $"Data Source={dbPath};Cache=Shared;";
    }

    /// <summary>
    /// Configure SQLite for WAL mode and performance
    /// </summary>
    public async Task ConfigureSqliteAsync()
    {
        try
        {
            // Enable WAL mode for concurrent access
            await Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            
            // Set synchronous mode to NORMAL for better performance
            await Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            
            // Set cache size to 10MB
            await Database.ExecuteSqlRawAsync("PRAGMA cache_size=10000;");
            
            // Enable foreign key constraints
            await Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
            
            // Set busy timeout to 30 seconds
            await Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");

            _logger?.LogInformation("SQLite database configured with WAL mode and performance optimizations");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to configure SQLite database");
            throw;
        }
    }

    /// <summary>
    /// Ensure database is created and configured
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        try
        {
            await Database.EnsureCreatedAsync();
            await ConfigureSqliteAsync();
            _logger?.LogInformation("Database created and configured successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create and configure database");
            throw;
        }
    }
}