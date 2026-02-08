using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseBackups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsAutomaticDailyBackup = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseBackups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseBackups_CreatedAt",
                table: "DatabaseBackups",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseBackups_IsAutomaticDailyBackup",
                table: "DatabaseBackups",
                column: "IsAutomaticDailyBackup");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseBackups_Type",
                table: "DatabaseBackups",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatabaseBackups");
        }
    }
}
