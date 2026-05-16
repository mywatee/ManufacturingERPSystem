using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IAuditLogService
{
    Task<List<AuditLog>> GetRecentAsync(int take = 200);
    Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, DateTime? startDate = null, DateTime? endDate = null);
    Task LogAsync(int? userId, string action, string tableName, string? oldValue = null, string? newValue = null);
    Task LogActionAsync(string action, string message, string? tableName = null);
}

