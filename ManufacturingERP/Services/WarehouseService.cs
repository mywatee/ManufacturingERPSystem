using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IActivityService _activityService;
    private readonly IAuditLogService _auditLogService;

    public WarehouseService(IDbContextFactory<ManufacturingContext> contextFactory, IActivityService activityService, IAuditLogService auditLogService)
    {
        _contextFactory = contextFactory;
        _activityService = activityService;
        _auditLogService = auditLogService;
    }

    public async Task<List<WarehouseConfig>> GetWarehousesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var warehouses = await context.Warehouses
            .Include(w => w.Manager).ThenInclude(u => u.Employee)
            .Include(w => w.Inventories)
            .ToListAsync();
            
        return warehouses.Select(w => new WarehouseConfig
        {
            Id = w.WarehouseId.ToString(),
            Code = w.Code ?? $"WH-{w.WarehouseId:D3}",
            Name = w.WarehouseName ?? "N/A",
            Location = w.Location ?? "N/A",
            Manager = w.Manager?.Employee?.FullName ?? "Chưa chỉ định",
            Capacity = (double)(w.Capacity ?? 0),
            Used = w.Inventories.Sum(i => (double)(i.CurrentStock ?? 0)),
            Status = w.Status ?? "Hoạt động"
        }).ToList();
    }

    public async Task<List<InventoryItemDisplay>> GetInventoryAsync(string warehouseName = "Tất cả")
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Inventories
            .Include(i => i.Material)
            .Include(i => i.Warehouse)
            .AsQueryable();

        if (!string.IsNullOrEmpty(warehouseName) && warehouseName != "Tất cả")
        {
            query = query.Where(i => i.Warehouse != null && i.Warehouse.WarehouseName == warehouseName);
        }

        var inventories = await query.ToListAsync();

        return inventories.Select(i => new InventoryItemDisplay
        {
            Id = $"{i.MaterialId}_{i.WarehouseId}",
            MaterialCode = i.Material?.MaterialCode ?? "N/A",
            MaterialName = i.Material?.MaterialName ?? "N/A",
            Warehouse = i.Warehouse?.WarehouseName ?? "Kho mặc định",
            CurrentQty = (double)(i.CurrentStock ?? 0),
            MinStock = (double)(i.Material?.MinStock ?? 0),
            MaxStock = (double)(i.Material?.MinStock ?? 0) * 2, // Default max as 2x min for visualization
            Unit = i.Material?.Unit ?? "Kg",
            UnitPrice = i.Material?.UnitPrice ?? 0,
            Status = CalculateStatus((double)(i.CurrentStock ?? 0), (double)(i.Material?.MinStock ?? 0))
        }).ToList();
    }

    private string CalculateStatus(double current, double min)
    {
        if (current <= 0) return "Hết hàng";
        if (current < min) return "Cảnh báo tồn thấp";
        return "An toàn";
    }

    public async Task<List<StockTransactionDisplay>> GetTransactionsAsync(string warehouseName = "Tất cả", int limit = 100)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.StockTransactions
            .Include(t => t.Material)
            .Include(t => t.Warehouse)
            .Include(t => t.TransByNavigation).ThenInclude(u => u.Employee)
            .OrderByDescending(t => t.TransDate)
            .AsNoTracking()
            .AsQueryable();

        // Note: Warehouse relation isn't directly on StockTransaction in our DB, 
        // but typically a transaction belongs to a warehouse. We'll filter if we can, else just return all.
        // For now, return all recent.
        var transactions = await query.Take(limit).ToListAsync();

        return transactions.Select(t => new StockTransactionDisplay
        {
            Id = t.TransactionId.ToString(),
            TransactionDate = t.TransDate?.ToString("yyyy-MM-dd HH:mm") ?? "N/A",
            Type = t.Type ?? "Nhập kho",
            MaterialCode = t.Material?.MaterialCode ?? "N/A",
            MaterialName = t.Material?.MaterialName ?? "N/A",
            Quantity = (double)(t.Quantity ?? 0),
            Unit = t.Material?.Unit ?? "Cái",
            Warehouse = t.Warehouse?.WarehouseName ?? "Tất cả",
            TransBy = t.TransByNavigation?.Employee?.FullName ?? "Hệ thống",
            ReferenceDoc = t.ReferenceCode,
            Notes = ""
        }).ToList();
    }

    public async Task<List<StockAlertDisplay>> GetStockAlertsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // 1. Lấy tất cả vật tư có định mức tồn kho tối thiểu
        var materials = await context.Materials
            .Include(m => m.Inventories)
            .ThenInclude(i => i.Warehouse)
            .Where(m => m.MinStock > 0)
            .ToListAsync();
            
        var alerts = new List<StockAlertDisplay>();
        
        foreach (var m in materials)
        {
            var minStock = (double)(m.MinStock ?? 0);
            
            // Trường hợp 1: Vật tư chưa từng có bản ghi trong bảng Inventories (chắc chắn là Hết hàng)
            if (m.Inventories == null || !m.Inventories.Any())
            {
                alerts.Add(new StockAlertDisplay
                {
                    Id = $"{m.MaterialId}_0",
                    MaterialCode = m.MaterialCode ?? "N/A",
                    MaterialName = m.MaterialName ?? "N/A",
                    Warehouse = "Chưa nhập kho",
                    CurrentQty = 0,
                    MinStock = minStock,
                    Unit = m.Unit ?? "Cái",
                    AlertLevel = "Nguy cấp",
                    SpecialStatus = "Hết hàng",
                    ExpectedEndRange = "Đã hết"
                });
                continue;
            }

            // Trường hợp 2: Có bản ghi tồn kho, kiểm tra từng kho
            foreach (var i in m.Inventories)
            {
                var current = (double)(i.CurrentStock ?? 0);
                if (current <= minStock)
                {
                    var level = current == 0 ? "Nguy cấp" : (current < minStock * 0.5 ? "Cảnh báo" : "Lưu ý");
                    var status = current == 0 ? "Hết hàng" : "Dưới mức tối thiểu";

                    alerts.Add(new StockAlertDisplay
                    {
                        Id = $"{m.MaterialId}_{i.WarehouseId}",
                        MaterialCode = m.MaterialCode ?? "N/A",
                        MaterialName = m.MaterialName ?? "N/A",
                        Warehouse = i.Warehouse?.WarehouseName ?? "N/A",
                        CurrentQty = current,
                        MinStock = minStock,
                        Unit = m.Unit ?? "Cái",
                        AlertLevel = level,
                        SpecialStatus = status,
                        ExpectedEndRange = current == 0 ? "Đã hết" : "Sắp hết"
                    });
                }
            }
        }

        return alerts.OrderByDescending(a => a.AlertLevel == "Nguy cấp")
                     .ThenBy(a => a.CurrentQty)
                     .ToList();
    }

    public async Task<bool> AddStockTransactionAsync(StockTransaction transaction)
    {
        if (transaction == null) return false;
        return await AddStockTransactionsAsync(new List<StockTransaction> { transaction });
    }

    public async Task<bool> AddStockTransactionsAsync(List<StockTransaction> transactions)
    {
        if (transactions == null || !transactions.Any()) return false;

        using var context = await _contextFactory.CreateDbContextAsync();
        using var dbTransaction = await context.Database.BeginTransactionAsync();

        try
        {
            foreach (var transaction in transactions)
            {
                if (transaction.TransDate == null || transaction.TransDate == default)
                {
                    transaction.TransDate = DateTime.Now;
                }
                context.StockTransactions.Add(transaction);

                // Update inventory for specific warehouse
                var inventory = await context.Inventories
                    .FirstOrDefaultAsync(i => i.MaterialId == transaction.MaterialId && i.WarehouseId == transaction.WarehouseId);

                if (inventory != null)
                {
                    if (transaction.Type == "Nhập kho")
                    {
                        inventory.CurrentStock = (inventory.CurrentStock ?? 0) + transaction.Quantity;
                    }
                    else if (transaction.Type == "Xuất kho")
                    {
                        if ((inventory.CurrentStock ?? 0) < transaction.Quantity) 
                        {
                            await dbTransaction.RollbackAsync();
                            return false; // Insufficient stock
                        }
                        inventory.CurrentStock -= transaction.Quantity;
                    }
                    inventory.LastUpdated = DateTime.Now;
                }
                else if (transaction.Type == "Nhập kho")
                {
                    // Create new inventory record for this warehouse
                    context.Inventories.Add(new Inventory
                    {
                        MaterialId = transaction.MaterialId ?? 0,
                        WarehouseId = transaction.WarehouseId ?? 0,
                        CurrentStock = transaction.Quantity,
                        LastUpdated = DateTime.Now
                    });
                }
                else
                {
                    await dbTransaction.RollbackAsync();
                    return false; // Can't export from a warehouse with no stock record
                }
            }

            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Log activity
            var first = transactions.First();
            string actionName = first.Type ?? "Giao dịch kho";
            string reference = first.ReferenceCode ?? "N/A";
            await _activityService.LogActivityAsync(actionName, $"[StockTransactions] {actionName} {transactions.Count} mặt hàng. Tham chiếu: {reference}", first.TransBy?.ToString());
            await _auditLogService.LogActionAsync(actionName.ToUpper(), $"{actionName} {transactions.Count} mặt hàng (Mã: {reference})", "StockTransactions");

            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> AddWarehouseAsync(Warehouse warehouse)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            // Log activity
            await _activityService.LogActivityAsync("Thêm", $"[Warehouses] Thêm nhà kho mới: {warehouse.WarehouseName}", "Hệ thống");
            await _auditLogService.LogActionAsync("THÊM KHO", $"Tạo nhà kho mới: {warehouse.WarehouseName} ({warehouse.Code})", "Warehouses");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateWarehouseAsync(Warehouse warehouse)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Warehouses.Update(warehouse);
            await context.SaveChangesAsync();

            // Log activity
            await _activityService.LogActivityAsync("Sửa", $"[Warehouses] Cập nhật thông tin nhà kho: {warehouse.WarehouseName}", "Hệ thống");
            await _auditLogService.LogActionAsync("CẬP NHẬT KHO", $"Sửa thông tin kho: {warehouse.WarehouseName}", "Warehouses");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteWarehouseAsync(int warehouseId)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var warehouse = await context.Warehouses.FindAsync(warehouseId);
            if (warehouse == null) return false;

            // Optional: Check for existing inventory before deleting
            var hasInventory = await context.Inventories.AnyAsync(i => i.WarehouseId == warehouseId);
            if (hasInventory) return false;

            context.Warehouses.Remove(warehouse);
            await context.SaveChangesAsync();

            // Log activity
            await _activityService.LogActivityAsync("Xóa", $"[Warehouses] Xóa nhà kho: {warehouse.WarehouseName}", "Hệ thống");
            await _auditLogService.LogActionAsync("XÓA KHO", $"Đã xóa kho: {warehouse.WarehouseName}", "Warehouses");

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Material>> GetAllMaterialsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Materials.OrderBy(m => m.MaterialName).ToListAsync();
    }

    public async Task<bool> TransferStockAsync(int fromWarehouseId, int toWarehouseId, int materialId, decimal quantity, int? userId)
    {
        if (fromWarehouseId == toWarehouseId) return false;

        using var context = await _contextFactory.CreateDbContextAsync();
        using var dbTransaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. Kiểm tra tồn kho tại kho xuất
            var sourceInv = await context.Inventories
                .FirstOrDefaultAsync(i => i.MaterialId == materialId && i.WarehouseId == fromWarehouseId);

            if (sourceInv == null || (sourceInv.CurrentStock ?? 0) < quantity)
            {
                return false; // Không đủ hàng để chuyển
            }

            // 2. Thực hiện XUẤT KHO từ kho nguồn
            sourceInv.CurrentStock -= quantity;
            sourceInv.LastUpdated = DateTime.Now;

            context.StockTransactions.Add(new StockTransaction
            {
                MaterialId = materialId,
                WarehouseId = fromWarehouseId,
                Quantity = quantity,
                Type = "Xuất kho",
                ReferenceCode = $"TRANSFER-OUT-TO-{toWarehouseId}",
                TransBy = userId,
                TransDate = DateTime.Now
            });

            // 3. Thực hiện NHẬP KHO vào kho đích
            var destInv = await context.Inventories
                .FirstOrDefaultAsync(i => i.MaterialId == materialId && i.WarehouseId == toWarehouseId);

            if (destInv != null)
            {
                destInv.CurrentStock = (destInv.CurrentStock ?? 0) + quantity;
                destInv.LastUpdated = DateTime.Now;
            }
            else
            {
                context.Inventories.Add(new Inventory
                {
                    MaterialId = materialId,
                    WarehouseId = toWarehouseId,
                    CurrentStock = quantity,
                    LastUpdated = DateTime.Now
                });
            }

            context.StockTransactions.Add(new StockTransaction
            {
                MaterialId = materialId,
                WarehouseId = toWarehouseId,
                Quantity = quantity,
                Type = "Nhập kho",
                ReferenceCode = $"TRANSFER-IN-FROM-{fromWarehouseId}",
                TransBy = userId,
                TransDate = DateTime.Now
            });

            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            
            await _auditLogService.LogActionAsync("CHUYỂN KHO", $"Chuyển {quantity} mặt hàng ID {materialId} từ kho {fromWarehouseId} sang {toWarehouseId}", "StockTransactions");
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> AdjustStockAsync(int warehouseId, int materialId, decimal actualQuantity, int? userId, string notes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var dbTransaction = await context.Database.BeginTransactionAsync();

        try
        {
            var inventory = await context.Inventories
                .FirstOrDefaultAsync(i => i.MaterialId == materialId && i.WarehouseId == warehouseId);

            decimal currentQty = inventory?.CurrentStock ?? 0;
            decimal diff = actualQuantity - currentQty;

            if (diff == 0) return true; // No change needed

            // 1. Update Inventory
            if (inventory != null)
            {
                inventory.CurrentStock = actualQuantity;
                inventory.LastUpdated = DateTime.Now;
            }
            else
            {
                context.Inventories.Add(new Inventory
                {
                    MaterialId = materialId,
                    WarehouseId = warehouseId,
                    CurrentStock = actualQuantity,
                    LastUpdated = DateTime.Now
                });
            }

            // 2. Add Audit Transaction
            context.StockTransactions.Add(new StockTransaction
            {
                MaterialId = materialId,
                WarehouseId = warehouseId,
                Quantity = Math.Abs(diff),
                Type = "Điều chỉnh",
                ReferenceCode = $"ADJUST-{(diff > 0 ? "INC" : "DEC")}-{DateTime.Now:yyyyMMdd}",
                TransBy = userId,
                TransDate = DateTime.Now
            });

            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            
            await _auditLogService.LogActionAsync("ĐIỀU CHỈNH KHO", $"Điều chỉnh kho {warehouseId}, mặt hàng {materialId}. Chênh lệch: {diff}", "Inventories");
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> CancelTransactionAsync(int transactionId, int userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var dbTransaction = await context.Database.BeginTransactionAsync();

        try
        {
            var original = await context.StockTransactions.FindAsync(transactionId);
            if (original == null) return false;

            // 1. Create a reverse transaction
            string reverseType = original.Type == "Nhập kho" ? "Xuất kho" : "Nhập kho";
            
            var reverse = new StockTransaction
            {
                MaterialId = original.MaterialId,
                WarehouseId = original.WarehouseId,
                Quantity = original.Quantity,
                Type = reverseType,
                ReferenceCode = "VOID-" + (original.ReferenceCode ?? original.TransactionId.ToString()),
                TransBy = userId,
                TransDate = DateTime.Now,
                Notes = $"Hủy giao dịch #{original.TransactionId}"
            };

            context.StockTransactions.Add(reverse);

            // 2. Update Inventory
            var inventory = await context.Inventories
                .FirstOrDefaultAsync(i => i.MaterialId == original.MaterialId && i.WarehouseId == original.WarehouseId);

            if (inventory != null)
            {
                if (reverseType == "Nhập kho")
                {
                    inventory.CurrentStock = (inventory.CurrentStock ?? 0) + original.Quantity;
                }
                else
                {
                    if ((inventory.CurrentStock ?? 0) < original.Quantity) return false; // Not enough to rollback
                    inventory.CurrentStock -= original.Quantity;
                }
                inventory.LastUpdated = DateTime.Now;
            }

            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            // Log activity
            await _activityService.LogActivityAsync("Hủy phiếu", $"[StockTransactions] Hủy giao dịch #{transactionId}. Tạo phiếu đảo: {reverse.ReferenceCode}", userId.ToString());
            await _auditLogService.LogActionAsync("HỦY PHIẾU KHO", $"Hủy giao dịch #{transactionId}, tạo phiếu đảo mã {reverse.ReferenceCode}", "StockTransactions");

            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }

    public async Task<List<StockTransaction>> GetTransactionsByReferenceAsync(string referenceCode)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // If referenceCode starts with ID:, search by ID instead of ReferenceCode
        if (referenceCode.StartsWith("ID:") && int.TryParse(referenceCode.Substring(3), out int id))
        {
            return await context.StockTransactions
                .Include(t => t.Material)
                .Include(t => t.Warehouse)
                .Include(t => t.Partner)
                .Where(t => t.TransactionId == id)
                .ToListAsync();
        }

        return await context.StockTransactions
            .Include(t => t.Material)
            .Include(t => t.Warehouse)
            .Include(t => t.Partner)
            .Where(t => t.ReferenceCode == referenceCode)
            .ToListAsync();
    }
}

