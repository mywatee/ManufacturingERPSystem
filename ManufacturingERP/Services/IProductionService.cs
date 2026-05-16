using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services
{
    public interface IProductionService
    {
        Task<List<WorkOrder>> GetRecentWorkOrdersAsync(int count);
        Task<List<WorkOrderProgress>> GetRecentProgressAsync(int count);
        Task<List<WorkOrder>> GetFilteredWorkOrdersAsync(DateTime start, DateTime end, string? status);
        Task<bool> CreateWorkOrderAsync(WorkOrder order);
        Task<bool> UpdateWorkOrderAsync(WorkOrder order);
        Task<bool> AddWorkOrderProgressAsync(WorkOrderProgress progress);
        Task<WorkOrder?> GetWorkOrderByCodeAsync(string code);
        Task<List<Material>> GetProductsAsync();
        Task<int> DeleteOldWorkOrdersAsync(DateTime beforeDate);
        Task<(int MaterialAlerts, double TodayProductivity, decimal MonthlyRevenue)> GetDashboardStatsAsync();
        Task<(List<int> ProductionValues, List<int> DefectValues, List<string> Labels)> GetProductionChartDataAsync(int days);
        Task<List<MaterialAvailability>> CheckMaterialAvailabilityAsync(int workOrderId);
    }

    public class MaterialAvailability
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
        public decimal AvailableQty { get; set; }
        public bool IsEnough => AvailableQty >= RequiredQty;
        public decimal Shortage => Math.Max(0, RequiredQty - AvailableQty);
        public string Unit { get; set; } = string.Empty;
    }
}

