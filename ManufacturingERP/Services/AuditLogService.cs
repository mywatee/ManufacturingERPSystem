using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IAuthService _authService;

    public AuditLogService(IDbContextFactory<ManufacturingContext> contextFactory, IAuthService authService)
    {
        _contextFactory = contextFactory;
        _authService = authService;
    }

    public async Task<List<AuditLog>> GetRecentAsync(int take = 200)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AuditLogs
            .AsNoTracking()
            .Include(l => l.User).ThenInclude(u => u.Employee)
            .OrderByDescending(l => l.Timestamp)
            .Take(take <= 0 ? 200 : take)
            .ToListAsync();
    }
    public async Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AuditLogs.AsNoTracking().Include(l => l.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(l => 
                l.Action.ToLower().Contains(searchLower) || 
                l.TableName.ToLower().Contains(searchLower) ||
                (l.OldValue != null && l.OldValue.ToLower().Contains(searchLower)) ||
                (l.NewValue != null && l.NewValue.ToLower().Contains(searchLower)) ||
                (l.User != null && l.User.Username.ToLower().Contains(searchLower)) ||
                (l.User != null && l.User.Employee != null && l.User.Employee.FullName.ToLower().Contains(searchLower))
            );
        }

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(l => l.Timestamp <= endOfDay);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
    public async Task LogAsync(int? userId, string action, string tableName, string? oldValue = null, string? newValue = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = tableName,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = DateTime.Now
        });
        await context.SaveChangesAsync();
    }

    public async Task LogActionAsync(string action, string message, string? tableName = null)
    {
        int? userId = _authService.CurrentUser?.UserId;
        // Now that NewValue is mapped, we can store the detailed message there
        await LogAsync(userId, action, tableName ?? "System", null, message);
    }
}
