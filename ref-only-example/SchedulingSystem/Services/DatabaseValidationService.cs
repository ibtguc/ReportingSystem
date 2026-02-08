using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SchedulingSystem.Data;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace SchedulingSystem.Services;

/// <summary>
/// Service for validating database schema matches the current DbContext model
/// Useful for catching schema drift when using EnsureCreated() instead of migrations
/// </summary>
public class DatabaseValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseValidationService> _logger;

    public DatabaseValidationService(
        ApplicationDbContext context,
        ILogger<DatabaseValidationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validates that the database schema matches the DbContext model
    /// </summary>
    /// <returns>Validation result with any issues found</returns>
    public async Task<DatabaseValidationResult> ValidateSchemaAsync()
    {
        var result = new DatabaseValidationResult { IsValid = true };

        try
        {
            _logger.LogInformation("Starting database schema validation...");

            // Check 1: Can connect to database
            if (!await CanConnectAsync())
            {
                result.IsValid = false;
                result.Errors.Add("Cannot connect to database");
                return result;
            }

            // Check 2: Get all entity types from the model
            var model = _context.Model;
            var entityTypes = model.GetEntityTypes().ToList();

            _logger.LogInformation("Validating {Count} entity types in DbContext model", entityTypes.Count);

            // Check 3: Validate each table exists
            var missingTables = new List<string>();
            var invalidColumns = new List<string>();

            foreach (var entityType in entityTypes)
            {
                // Get table name - first try annotation, then fall back to class name
                var tableName = entityType.FindAnnotation("Relational:TableName")?.Value?.ToString()
                                ?? entityType.ClrType.Name;

                if (string.IsNullOrEmpty(tableName))
                    continue;

                _logger.LogDebug("Checking table: {TableName}", tableName);

                // Check if table exists
                if (!await TableExistsAsync(tableName))
                {
                    missingTables.Add(tableName);
                    result.IsValid = false;
                    continue;
                }

                // Check columns for this table
                // Use property name as column name (default EF Core behavior)
                var modelColumns = entityType.GetProperties()
                    .Select(p => p.Name)
                    .ToList();

                var dbColumns = await GetTableColumnsAsync(tableName);

                // Check for missing columns
                foreach (var modelColumn in modelColumns)
                {
                    if (!dbColumns.Contains(modelColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        invalidColumns.Add($"{tableName}.{modelColumn} (missing in database)");
                        result.IsValid = false;
                    }
                }

                // Check for extra columns (warning only)
                foreach (var dbColumn in dbColumns)
                {
                    if (!modelColumns.Contains(dbColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Warnings.Add($"{tableName}.{dbColumn} (exists in database but not in model)");
                    }
                }
            }

            // Add errors for missing tables
            if (missingTables.Any())
            {
                result.Errors.Add($"Missing tables: {string.Join(", ", missingTables)}");
            }

            // Add errors for invalid columns
            if (invalidColumns.Any())
            {
                result.Errors.Add($"Missing columns: {string.Join(", ", invalidColumns)}");
            }

            // Summary
            if (result.IsValid)
            {
                _logger.LogInformation("✓ Database schema validation passed - all tables and columns match the model");
            }
            else
            {
                _logger.LogWarning("✗ Database schema validation failed - {ErrorCount} errors found", result.Errors.Count);
            }

            if (result.Warnings.Any())
            {
                _logger.LogWarning("Database schema validation found {WarningCount} warnings", result.Warnings.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database schema validation");
            result.IsValid = false;
            result.Errors.Add($"Validation exception: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Checks if the database can be connected to
    /// </summary>
    private async Task<bool> CanConnectAsync()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to database");
            return false;
        }
    }

    /// <summary>
    /// Checks if a specific table exists in the database
    /// </summary>
    private async Task<bool> TableExistsAsync(string tableName)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            await EnsureConnectionOpenAsync(connection);

            var command = connection.CreateCommand();

            // SQLite-specific query (also works with SQL Server with slight modifications)
            var databaseProvider = _context.Database.ProviderName;

            if (databaseProvider?.Contains("Sqlite") == true)
            {
                command.CommandText = @"
                    SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE type='table' AND name=@tableName";
            }
            else if (databaseProvider?.Contains("SqlServer") == true)
            {
                command.CommandText = @"
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME=@tableName";
            }
            else
            {
                _logger.LogWarning("Unknown database provider: {Provider}, skipping table existence check", databaseProvider);
                return true; // Assume exists if we can't check
            }

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if table {TableName} exists", tableName);
            return false;
        }
    }

    /// <summary>
    /// Gets all column names for a specific table
    /// </summary>
    private async Task<List<string>> GetTableColumnsAsync(string tableName)
    {
        var columns = new List<string>();

        try
        {
            var connection = _context.Database.GetDbConnection();
            await EnsureConnectionOpenAsync(connection);

            var command = connection.CreateCommand();
            var databaseProvider = _context.Database.ProviderName;

            if (databaseProvider?.Contains("Sqlite") == true)
            {
                command.CommandText = $"PRAGMA table_info({tableName})";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(1)); // Column name is at index 1
                }
            }
            else if (databaseProvider?.Contains("SqlServer") == true)
            {
                command.CommandText = @"
                    SELECT COLUMN_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME=@tableName";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(0));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting columns for table {TableName}", tableName);
        }

        return columns;
    }

    /// <summary>
    /// Ensures the database connection is open
    /// </summary>
    private async Task EnsureConnectionOpenAsync(DbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
    }
}

/// <summary>
/// Result of database schema validation
/// </summary>
public class DatabaseValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public string GetSummary()
    {
        var summary = new List<string>();

        if (IsValid)
        {
            summary.Add("✓ Database schema is valid");
        }
        else
        {
            summary.Add("✗ Database schema validation failed");
        }

        if (Errors.Any())
        {
            summary.Add($"\nErrors ({Errors.Count}):");
            summary.AddRange(Errors.Select(e => $"  - {e}"));
        }

        if (Warnings.Any())
        {
            summary.Add($"\nWarnings ({Warnings.Count}):");
            summary.AddRange(Warnings.Select(w => $"  - {w}"));
        }

        return string.Join("\n", summary);
    }
}

/// <summary>
/// Configuration options for database validation
/// </summary>
public class DatabaseValidationOptions
{
    public bool Enabled { get; set; } = false;
    public bool FailOnMismatch { get; set; } = false;
}
