using System.Threading.Tasks;

namespace ManufacturingERP.Services;

public interface IBackupService
{
    Task<string> BackupDatabaseAsync(string filePath);
    Task RestoreDatabaseAsync(string filePath);
    bool IsValidBackupFile(string filePath);
}
