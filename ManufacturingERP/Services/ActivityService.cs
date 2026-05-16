using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public class ActivityService : IActivityService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly INotificationService _notificationService;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;

    public ActivityService(
        IDbContextFactory<ManufacturingContext> contextFactory,
        INotificationService notificationService,
        IAuthService authService,
        IAuditLogService auditLogService)
    {
        _contextFactory = contextFactory;
        _notificationService = notificationService;
        _authService = authService;
        _auditLogService = auditLogService;
    }

    public async Task LogActivityAsync(string type, string content, string? user = null)
    {
        if (type == "Cập nhật") type = "Sửa";

        try
        {
            var actor = _authService.CurrentUser;
            await _auditLogService.LogAsync(actor?.UserId, type, "System Activity", "-", content);
        }
        catch (Exception ex)
        {
            _notificationService.ShowWarning($"Không thể ghi nhật ký hoạt động: {ex.Message}");
        }
    }

    public async Task<List<ActivityLog>> GetRecentActivitiesAsync(int count)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var auditLogs = await context.AuditLogs
            .Include(a => a.User)
                .ThenInclude(u => u.Employee)
            .AsNoTracking()
            .OrderByDescending(a => a.LogId)
            .Take(count)
            .ToListAsync();
            
        return auditLogs.Select(a => new ActivityLog
        {
            LogId = a.LogId,
            ActivityType = a.Action ?? "Thao tác",
            Content = $"[{a.TableName}] {a.NewValue}",
            PerformedBy = a.User?.Employee?.FullName ?? a.User?.Username ?? "System",
            Timestamp = a.Timestamp
        }).ToList();
    }

    public async Task<List<ActivityLog>> GetFilteredActivitiesAsync(DateTime start, DateTime end, string type)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AuditLogs
            .Include(a => a.User)
                .ThenInclude(u => u.Employee)
            .AsNoTracking()
            .Where(a => a.Timestamp >= start.Date && a.Timestamp < end.Date.AddDays(1));

        if (type != "Tất cả")
        {
            if (type == "Sửa")
            {
                query = query.Where(a => a.Action == "Sửa" || a.Action == "Cập nhật");
            }
            else
            {
                query = query.Where(a => a.Action == type);
            }
        }

        var results = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

        return results.Select(a => new ActivityLog
        {
            LogId = a.LogId,
            ActivityType = a.Action == "Cập nhật" ? "Sửa" : (a.Action ?? "Thao tác"),
            Content = $"[{a.TableName}] {a.NewValue}",
            PerformedBy = a.User?.Employee?.FullName ?? a.User?.Username ?? "System",
            Timestamp = a.Timestamp
        }).ToList();
    }
}
