using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseITAgent.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackupJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SourcePaths = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalFiles = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulFiles = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedFiles = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Progress = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ThreadId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    From = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    To = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Cc = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Bcc = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFlagged = table.Column<bool>(type: "INTEGER", nullable: false),
                    Folder = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ImapUid = table.Column<uint>(type: "INTEGER", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChunkIds = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ChunkCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    SourceNodeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BackupJobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileVersions", x => x.Id);
                    table.UniqueConstraint("AK_FileVersions_FileHash", x => x.FileHash);
                });

            migrationBuilder.CreateTable(
                name: "LogEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Component = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NodeStatus",
                columns: table => new
                {
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metrics_CpuUsage = table.Column<double>(type: "REAL", nullable: false),
                    Metrics_MemoryUsage = table.Column<double>(type: "REAL", nullable: false),
                    Metrics_TotalMemoryMB = table.Column<long>(type: "INTEGER", nullable: false),
                    Metrics_AvailableMemoryMB = table.Column<long>(type: "INTEGER", nullable: false),
                    Metrics_DiskUsage = table.Column<double>(type: "REAL", nullable: false),
                    Metrics_TotalDiskGB = table.Column<long>(type: "INTEGER", nullable: false),
                    Metrics_AvailableDiskGB = table.Column<long>(type: "INTEGER", nullable: false),
                    Metrics_NetworkBytesIn = table.Column<long>(type: "INTEGER", nullable: false),
                    Metrics_NetworkBytesOut = table.Column<long>(type: "INTEGER", nullable: false),
                    Metrics_ProcessCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Metrics_UptimeHours = table.Column<double>(type: "REAL", nullable: false),
                    Metrics_Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metrics_CustomMetrics = table.Column<string>(type: "TEXT", nullable: true),
                    ActiveServices = table.Column<string>(type: "TEXT", nullable: false),
                    Alerts = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OsInfo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MachineName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CurrentUser = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsIdle = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvailableBackupStorageGB = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeStatus", x => x.NodeId);
                });

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmailId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReferenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_EmailMessages_EmailId",
                        column: x => x.EmailId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackupChunks",
                columns: table => new
                {
                    ChunkId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ChunkHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReplicatedNodes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TargetReplicas = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentReplicas = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastVerified = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupChunks", x => x.ChunkId);
                    table.ForeignKey(
                        name: "FK_BackupChunks_FileVersions_FileHash",
                        column: x => x.FileHash,
                        principalTable: "FileVersions",
                        principalColumn: "FileHash",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupChunks_ChunkHash",
                table: "BackupChunks",
                column: "ChunkHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackupChunks_FileHash",
                table: "BackupChunks",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_BackupChunks_FileHash_SequenceNumber",
                table: "BackupChunks",
                columns: new[] { "FileHash", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_NodeId",
                table: "BackupJobs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_StartTime",
                table: "BackupJobs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_BackupJobs_Status",
                table: "BackupJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_ContentHash",
                table: "EmailAttachments",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailId",
                table: "EmailAttachments",
                column: "EmailId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Folder",
                table: "EmailMessages",
                column: "Folder");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Folder_ImapUid",
                table: "EmailMessages",
                columns: new[] { "Folder", "ImapUid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ImapUid",
                table: "EmailMessages",
                column: "ImapUid");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ThreadId",
                table: "EmailMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_Timestamp",
                table: "EmailMessages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_BackupJobId",
                table: "FileVersions",
                column: "BackupJobId");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_FileHash",
                table: "FileVersions",
                column: "FileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_FilePath",
                table: "FileVersions",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_FileVersions_SourceNodeId",
                table: "FileVersions",
                column: "SourceNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Component",
                table: "LogEntries",
                column: "Component");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Level",
                table: "LogEntries",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_NodeId",
                table: "LogEntries",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LogEntries_Timestamp",
                table: "LogEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_NodeStatus_LastHeartbeat",
                table: "NodeStatus",
                column: "LastHeartbeat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupChunks");

            migrationBuilder.DropTable(
                name: "BackupJobs");

            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "LogEntries");

            migrationBuilder.DropTable(
                name: "NodeStatus");

            migrationBuilder.DropTable(
                name: "FileVersions");

            migrationBuilder.DropTable(
                name: "EmailMessages");
        }
    }
}
