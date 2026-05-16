using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services
{
    public class ProductionService : IProductionService
    {
        private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
        private readonly IAuthService _authService;
        private readonly IMasterDataService _masterDataService;
        private readonly IWarehouseService _warehouseService;

        public ProductionService(IDbContextFactory<ManufacturingContext> contextFactory, IAuthService authService, IMasterDataService masterDataService, IWarehouseService warehouseService)
        {
            _contextFactory = contextFactory;
            _authService = authService;
            _masterDataService = masterDataService;
            _warehouseService = warehouseService;
        }

        public async Task<List<WorkOrder>> GetRecentWorkOrdersAsync(int count)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.WorkOrders
                .Include(w => w.WorkOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(w => w.WorkOrderProgresses)
                .OrderByDescending(w => w.IsUrgent)
                .ThenByDescending(w => w.Woid)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<WorkOrder>> GetFilteredWorkOrdersAsync(DateTime start, DateTime end, string? status)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.WorkOrders
                .Include(w => w.WorkOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(w => w.WorkOrderProgresses)
                .Include(w => w.CreatedByNavigation) // Include to get User Name
                .AsQueryable();

            // Normalize dates
            var startDate = start.Date;
            var endDate = end.Date.AddDays(1);

            // Filter primarily by CreatedAt as requested for "Creation Time" reporting
            query = query.Where(w => w.CreatedAt != null && w.CreatedAt >= startDate && w.CreatedAt < endDate);

            // Filter by status if specified
            if (!string.IsNullOrEmpty(status) && status != "Tất cả trạng thái")
            {
                var possibleStatuses = new List<string> { status };
                if (status == "Đang sản xuất") possibleStatuses.Add("Running");
                else if (status == "Hoàn thành") possibleStatuses.Add("Completed");
                else if (status == "Tạm dừng") possibleStatuses.Add("Paused");
                else if (status == "Chờ duyệt" || status == "Chờ") possibleStatuses.Add("Planned");
                
                // Chỉ lấy các lệnh có khả năng thực hiện QC (Đang làm, Đã xong, Tạm dừng)
                var validStatuses = new[] { "Running", "Đang sản xuất", "Completed", "Hoàn thành", "Paused", "Tạm dừng" };
                query = query.Where(w => validStatuses.Contains(w.Status));
            }

            return await query.OrderByDescending(w => w.Woid).ToListAsync();
        }

        public async Task<bool> CreateWorkOrderAsync(WorkOrder order)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Optimization: Automatically fill metadata
                order.CreatedAt = DateTime.Now;
                order.CreatedBy = _authService.CurrentUser?.UserId;
                order.Status = "Chờ";
                
                // Optimization: Sync first item to main table for quick view
                if (order.WorkOrderItems != null && order.WorkOrderItems.Any())
                {
                    var firstItem = order.WorkOrderItems.First();
                    order.ProductId = firstItem.ProductId;
                    order.TargetQty = order.WorkOrderItems.Sum(i => i.TargetQty);
                    order.ActualQty = 0;
                }

                context.WorkOrders.Add(order);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Temporary debug log
                try { System.IO.File.WriteAllText(@"D:\DoAn\DoAnCoSo\ManufacturingERP\debug_error.txt", ex.ToString()); } catch { }
                return false;
            }
        }

        public async Task<bool> UpdateWorkOrderAsync(WorkOrder order)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Get existing order from DB with its items to handle replacement
                var existingOrder = await context.WorkOrders
                    .Include(w => w.WorkOrderItems)
                    .FirstOrDefaultAsync(w => w.Woid == order.Woid);

                if (existingOrder == null) return false;

                // 1. Update header fields
                existingOrder.Wocode = order.Wocode;
                existingOrder.Status = order.Status;
                existingOrder.IsUrgent = order.IsUrgent;
                existingOrder.StartDate = order.StartDate;
                existingOrder.EndDate = order.EndDate;
                
                // 2. Cập nhật thông minh (Merge) thay vì Xóa sạch
                foreach (var incomingItem in order.WorkOrderItems)
                {
                    var existingItem = existingOrder.WorkOrderItems.FirstOrDefault(i => i.ProductId == incomingItem.ProductId);
                    if (existingItem != null)
                    {
                        existingItem.TargetQty = incomingItem.TargetQty;
                        existingItem.Status = incomingItem.Status ?? existingItem.Status;
                    }
                    else
                    {
                        existingOrder.WorkOrderItems.Add(new WorkOrderItem
                        {
                            ProductId = incomingItem.ProductId,
                            TargetQty = incomingItem.TargetQty,
                            ActualQty = incomingItem.ActualQty,
                            Status = incomingItem.Status ?? "Planned"
                        });
                    }
                }
                
                // Xóa những item cũ không còn tồn tại trong list gửi lên
                var incomingProductIds = order.WorkOrderItems.Select(i => i.ProductId).ToList();
                var itemsToRemove = existingOrder.WorkOrderItems.Where(i => !incomingProductIds.Contains(i.ProductId ?? 0)).ToList();
                if (itemsToRemove.Any())
                {
                    context.WorkOrderItems.RemoveRange(itemsToRemove);
                }

                // 3. Sync summary fields for Dashboard
                if (existingOrder.WorkOrderItems.Any())
                {
                    var firstItem = existingOrder.WorkOrderItems.First();
                    existingOrder.ProductId = firstItem.ProductId;
                    existingOrder.TargetQty = existingOrder.WorkOrderItems.Sum(i => i.TargetQty);
                }

                await context.SaveChangesAsync();

                // 4. Nếu trạng thái là Hoàn thành, tiến hành tác động kho
                if (order.Status == "Completed" || order.Status == "Hoàn thành")
                {
                    await ProcessInventoryImpactAsync(existingOrder);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> AddWorkOrderProgressAsync(WorkOrderProgress progress)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // CHỈ LƯU VẾT LỊCH SỬ TIẾN ĐỘ, KHÔNG CẬP NHẬT TRỰC TIẾP VÀO ACTUALQTY CỦA LỆNH
                // (Vì ActualQty hiện tại sẽ dùng để lưu trữ sản lượng đã qua QC)
                context.WorkOrderProgresses.Add(progress);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task ProcessInventoryImpactAsync(WorkOrder order)
        {
            if (order.WorkOrderItems == null || !order.WorkOrderItems.Any()) return;

            // Lấy kho mặc định (Lấy kho đầu tiên trong hệ thống để thực hiện giao dịch tự động)
            var warehouses = await _warehouseService.GetWarehousesAsync();
            var defaultWarehouse = warehouses.FirstOrDefault();
            if (defaultWarehouse == null) return;

            if (!int.TryParse(defaultWarehouse.Id, out int warehouseId)) return;

            foreach (var item in order.WorkOrderItems)
            {
                if (item.ActualQty <= 0) continue;

                // 1. NHẬP KHO THÀNH PHẨM
                await _warehouseService.AddStockTransactionAsync(new StockTransaction
                {
                    MaterialId = item.ProductId,
                    WarehouseId = warehouseId,
                    Quantity = (decimal)item.ActualQty,
                    Type = "Nhập kho",
                    ReferenceCode = order.Wocode,
                    TransBy = _authService.CurrentUser?.UserId,
                    TransDate = DateTime.Now
                });

                // 2. XUẤT KHO NGUYÊN LIỆU (Dựa trên BOM)
                // Lấy thông tin vật tư để có mã MaterialCode
                var product = await _masterDataService.GetMaterialByCodeAsync(item.Product?.MaterialCode ?? "");
                if (product != null)
                {
                    var bomItems = await _masterDataService.GetBomByParentCodeAsync(product.MaterialCode);
                    if (bomItems != null && bomItems.Any())
                    {
                        foreach (var bom in bomItems)
                        {
                            decimal requiredQty = (decimal)item.ActualQty * bom.QuantityPerUnit;
                            
                            await _warehouseService.AddStockTransactionAsync(new StockTransaction
                            {
                                MaterialId = bom.ChildId,
                                WarehouseId = warehouseId,
                                Quantity = requiredQty,
                                Type = "Xuất kho",
                                ReferenceCode = order.Wocode,
                                TransBy = _authService.CurrentUser?.UserId,
                                TransDate = DateTime.Now
                            });
                        }
                    }
                }
            }
        }

        public async Task<WorkOrder?> GetWorkOrderByCodeAsync(string code)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.WorkOrders
                .Include(w => w.WorkOrderItems)
                    .ThenInclude(i => i.Product)
                .Include(w => w.WorkOrderItems)
                    .ThenInclude(i => i.WorkOrderProgresses)
                .Include(w => w.WorkOrderItems)
                    .ThenInclude(i => i.QualityControls)
                .Include(w => w.WorkOrderProgresses)
                .Include(w => w.QualityControls)
                .Include(w => w.CreatedByNavigation)
                .FirstOrDefaultAsync(w => w.Wocode == code);


        }

        public async Task<List<Material>> GetProductsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Materials
                .Where(m => m.Category == "Thành phẩm/BTP" || m.Category == "Thành phẩm")
                .ToListAsync();
        }

        public async Task<int> DeleteOldWorkOrdersAsync(DateTime beforeDate)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                var ordersToDelete = await context.WorkOrders
                    .Where(w => w.CreatedAt < beforeDate)
                    .ToListAsync();

                if (!ordersToDelete.Any()) return 0;

                var orderIds = ordersToDelete.Select(w => w.Woid).ToList();

                // Delete related records
                var items = await context.WorkOrderItems.Where(i => orderIds.Contains(i.WorkOrderId)).ToListAsync();
                var progress = await context.WorkOrderProgresses.Where(p => p.Woid != null && orderIds.Contains(p.Woid.Value)).ToListAsync();
                var qc = await context.QualityControls.Where(q => q.Woid != null && orderIds.Contains(q.Woid.Value)).ToListAsync();

                context.WorkOrderItems.RemoveRange(items);
                context.WorkOrderProgresses.RemoveRange(progress);
                context.QualityControls.RemoveRange(qc);
                
                context.WorkOrders.RemoveRange(ordersToDelete);

                return await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public async Task<(int MaterialAlerts, double TodayProductivity, decimal MonthlyRevenue)> GetDashboardStatsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // 1. Cảnh báo vật tư
            int alerts = await context.Materials
                .Include(m => m.Inventories)
                .Where(m => m.Inventories.Sum(i => i.CurrentStock ?? 0) < (decimal)(m.MinStock ?? 10))
                .CountAsync();

            // 2. Năng suất hôm nay
            var today = DateTime.Today;
            var activeOrders = await context.WorkOrders
                .Where(w => w.Status == "Running" || w.Status == "Đang sản xuất" || (w.Status == "Completed" && w.EndDate >= today))
                .ToListAsync();

            double productivity = 0;
            if (activeOrders.Any())
            {
                int totalTarget = activeOrders.Sum(w => w.TargetQty ?? 0);
                int totalActual = activeOrders.Sum(w => w.ActualQty ?? 0);
                if (totalTarget > 0)
                {
                    productivity = Math.Round((double)totalActual / totalTarget * 100, 1);
                }
            }

            // 3. Doanh thu tháng (Lấy giá từ logic kinh doanh - Tạm thời dùng hằng số tập trung)
            const decimal ESTIMATED_PRICE_PER_UNIT = 1250000m; 
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var completedQty = await context.WorkOrders
                .Where(w => (w.Status == "Completed" || w.Status == "Hoàn thành") && w.EndDate >= startOfMonth)
                .SumAsync(w => w.ActualQty ?? 0);

            decimal revenue = completedQty * ESTIMATED_PRICE_PER_UNIT;

            return (alerts, productivity, revenue);
        }

        public async Task<List<WorkOrderProgress>> GetRecentProgressAsync(int count)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.WorkOrderProgresses
                .Include(p => p.Wo)
                .Include(p => p.Worker)
                .OrderByDescending(p => p.EndTime ?? p.StartTime ?? DateTime.Now)
                .Take(count)
                .ToListAsync();
        }

        public async Task<(List<int> ProductionValues, List<int> DefectValues, List<string> Labels)> GetProductionChartDataAsync(int days)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var startDate = DateTime.Today.AddDays(-days + 1);
            
            // Query completed orders or progresses within the date range
            var orders = await context.WorkOrders
                .Where(w => w.EndDate >= startDate && w.EndDate <= DateTime.Today)
                .ToListAsync();

            var productionValues = new List<int>();
            var defectValues = new List<int>(); // Since we don't have DefectQty yet, mock it based on ActualQty
            var labels = new List<string>();

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                labels.Add(date.ToString("dd/MM"));
                
                var dailyOrders = orders.Where(w => w.EndDate?.Date == date).ToList();
                int dailyProd = dailyOrders.Sum(w => w.ActualQty ?? 0);
                productionValues.Add(dailyProd);
                
                // Mock defect as ~2% of production for now until Phase 2
                defectValues.Add((int)(dailyProd * 0.02)); 
            }

            return (productionValues, defectValues, labels);
        }


        public async Task<List<MaterialAvailability>> CheckMaterialAvailabilityAsync(int workOrderId)

        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var order = await context.WorkOrders
                .Include(w => w.WorkOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(w => w.Woid == workOrderId);

            if (order == null) return new List<MaterialAvailability>();

            var neededMaterials = new Dictionary<int, (string Code, string Name, string Unit, decimal TotalRequired)>();

            foreach (var item in order.WorkOrderItems)
            {
                if (item.ProductId == null) continue;
                
                // Get BOM for this product
                var bomItems = await context.Boms
                    .Include(b => b.Child)
                    .Where(b => b.ParentId == item.ProductId)
                    .ToListAsync();

                foreach (var bom in bomItems)
                {
                    if (bom.ChildId == null) continue;
                    
                    decimal required = (decimal)item.TargetQty * bom.QuantityPerUnit;
                    
                    if (neededMaterials.ContainsKey(bom.ChildId.Value))
                    {
                        var existing = neededMaterials[bom.ChildId.Value];
                        neededMaterials[bom.ChildId.Value] = (existing.Code, existing.Name, existing.Unit, existing.TotalRequired + required);
                    }
                    else
                    {
                        neededMaterials[bom.ChildId.Value] = (bom.Child?.MaterialCode ?? "", bom.Child?.MaterialName ?? "", bom.Child?.Unit ?? "", required);
                    }
                }
            }

            var results = new List<MaterialAvailability>();

            foreach (var mat in neededMaterials)
            {
                // Check inventory across all warehouses
                var available = await context.Inventories
                    .Where(i => i.MaterialId == mat.Key)
                    .SumAsync(i => i.CurrentStock ?? 0);

                results.Add(new MaterialAvailability
                {
                    MaterialCode = mat.Value.Code,
                    MaterialName = mat.Value.Name,
                    RequiredQty = mat.Value.TotalRequired,
                    AvailableQty = (decimal)available,
                    Unit = mat.Value.Unit
                });
            }

            return results;
        }
    }
}

