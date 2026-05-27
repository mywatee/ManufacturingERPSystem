using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ManufacturingERP.Core;

namespace ManufacturingERP.Services;

public class BackupService : IBackupService
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public BackupService()
    {
        _connectionString = ConnectionStrings.Default;
        _databaseName = GetDatabaseName(_connectionString);
    }

    private static string GetDatabaseName(string connStr)
    {
        var builder = new SqlConnectionStringBuilder(connStr);
        return builder.InitialCatalog;
    }

    public async Task<string> BackupDatabaseAsync(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var backupQuery = $@"
            BACKUP DATABASE [{_databaseName}]
            TO DISK = @filePath
            WITH FORMAT, NAME = 'ManufacturingERP Backup', COMPRESSION";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(backupQuery, conn);
        cmd.Parameters.AddWithValue("@filePath", filePath);
        await cmd.ExecuteNonQueryAsync();

        return filePath;
    }

    public async Task RestoreDatabaseAsync(string filePath)
    {
        var restoreQuery = $@"
            USE master;
            ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            RESTORE DATABASE [{_databaseName}]
            FROM DISK = @filePath
            WITH REPLACE, RECOVERY;
            ALTER DATABASE [{_databaseName}] SET MULTI_USER;";

        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = new SqlCommand(restoreQuery, conn);
        cmd.Parameters.AddWithValue("@filePath", filePath);
        await cmd.ExecuteNonQueryAsync();
    }

    public bool IsValidBackupFile(string filePath)
    {
        return File.Exists(filePath) &&
               string.Equals(Path.GetExtension(filePath), ".bak", StringComparison.OrdinalIgnoreCase);
    }
}
