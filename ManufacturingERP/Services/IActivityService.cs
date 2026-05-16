using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IActivityService
{
    Task LogActivityAsync(string type, string content, string? user = null);
    Task<List<ActivityLog>> GetRecentActivitiesAsync(int count);
    Task<List<ActivityLog>> GetFilteredActivitiesAsync(DateTime start, DateTime end, string type);
}
