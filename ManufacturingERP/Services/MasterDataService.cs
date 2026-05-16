using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManufacturingERP.Services;

public interface IMasterDataService
{
    Task<List<Material>> GetAllMaterialsAsync();
    Task<bool> AddMaterialAsync(Material material);
    Task<bool> UpdateMaterialAsync(Material material);
    Task<bool> IsMaterialCodeExistsAsync(string code);
    Task<Material?> GetMaterialByCodeAsync(string code);
    Task<(bool Success, string Message)> DeleteMaterialDetailedAsync(int materialId);
    
    // BOM Methods
    Task<List<Bom>> GetBomByParentCodeAsync(string parentCode);
    Task<bool> AddBomItemAsync(Bom bom);
    Task<bool> DeleteBomItemAsync(int bomId);

    // Routing Methods
    Task<List<Routing>> GetRoutingByParentCodeAsync(string parentCode);
    Task<bool> AddRoutingStepAsync(Routing step);
    Task<bool> UpdateRoutingStepAsync(Routing step);
    Task<bool> DeleteRoutingByProductIdAsync(int productId);
    Task<List<Bom>> GetAllBomsAsync();
    Task<List<Routing>> GetAllRoutingsAsync();
    Task<bool> UpdateBomItemAsync(Bom bom);
    Task<bool> IsCircularReferenceAsync(int parentId, int childId);
}

public class MasterDataService : IMasterDataService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IAuditLogService _auditLogService;

    public MasterDataService(IDbContextFactory<ManufacturingContext> contextFactory, IAuditLogService auditLogService)
    {
        _contextFactory = contextFactory;
        _auditLogService = auditLogService;
    }

    public async Task<List<Material>> GetAllMaterialsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Materials
            .Include(m => m.Inventories)
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsMaterialCodeExistsAsync(string code)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Materials.AsNoTracking().AnyAsync(m => m.MaterialCode == code);
    }

    public async Task<Material?> GetMaterialByCodeAsync(string code)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Materials.AsNoTracking().FirstOrDefaultAsync(m => m.MaterialCode == code);
    }

    public async Task<bool> AddMaterialAsync(Material material)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            material.CreatedAt = DateTime.Now;
            context.Materials.Add(material);
            await context.SaveChangesAsync();
            await _auditLogService.LogActionAsync("Thêm mới", $"Vật tư: {material.MaterialCode} - {material.MaterialName}", "MasterData");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateMaterialAsync(Material material)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var local = context.Materials.Local.FirstOrDefault(m => m.MaterialId == material.MaterialId);
            if (local != null)
                context.Entry(local).CurrentValues.SetValues(material);
            else
                context.Entry(material).State = EntityState.Modified;

            await context.SaveChangesAsync();
            await _auditLogService.LogActionAsync("Cập nhật", $"Vật tư: {material.MaterialCode}", "MasterData");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> DeleteMaterialDetailedAsync(int materialId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var material = await context.Materials
                .Include(m => m.Inventories)
                .FirstOrDefaultAsync(m => m.MaterialId == materialId);
                
            if (material == null) 
                return (false, "Không tìm thấy vật tư cần xóa.");

            // 1. Kiểm tra ràng buộc BOM (Vật tư này là cha của linh kiện khác)
            var hasBomParent = await context.Boms.AnyAsync(b => b.ParentId == materialId);
            if (hasBomParent)
                return (false, $"Vật tư '{material.MaterialCode}' đang được thiết lập Định mức (BOM). Hãy xóa định mức trước khi xóa vật tư.");

            // 2. Kiểm tra ràng buộc BOM (Vật tư này là linh kiện trong BOM khác)
            var hasBomChild = await context.Boms.AnyAsync(b => b.ChildId == materialId);
            if (hasBomChild)
                return (false, $"Vật tư '{material.MaterialCode}' đang là linh kiện trong các định mức khác. Hãy gỡ vật tư này khỏi các định mức đó trước.");

            // 3. Kiểm tra ràng buộc Quy trình (Routing)
            var hasRouting = await context.Routings.AnyAsync(r => r.ProductId == materialId);
            if (hasRouting)
                return (false, $"Vật tư '{material.MaterialCode}' đã có thiết lập Quy trình sản xuất. Hãy xóa quy trình trước.");

            // 4. Kiểm tra ràng buộc Lệnh sản xuất (Work Orders)
            var hasWorkOrders = await context.WorkOrderItems.AnyAsync(wo => wo.ProductId == materialId);
            if (hasWorkOrders)
                return (false, $"Vật tư '{material.MaterialCode}' đã phát sinh trong các Lệnh sản xuất. Không thể xóa dữ liệu đã có giao dịch.");

            // 5. Kiểm tra ràng buộc Giao dịch kho (Stock Transactions)
            var hasTransactions = await context.StockTransactions.AnyAsync(st => st.MaterialId == materialId);
            if (hasTransactions)
                return (false, $"Vật tư '{material.MaterialCode}' đã có lịch sử giao dịch kho. Không thể xóa để đảm bảo tính nhất quán dữ liệu.");

            // 6. Xóa Inventory liên quan trước (vì quan hệ 1-1)
            if (material.Inventories != null && material.Inventories.Any())
            {
                context.Inventories.RemoveRange(material.Inventories);
            }

            string code = material.MaterialCode;
            context.Materials.Remove(material);
            await context.SaveChangesAsync();
            await _auditLogService.LogActionAsync("Xóa", $"Vật tư: {code}", "MasterData");
            
            return (true, "Xóa vật tư thành công.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi hệ thống khi xóa vật tư: {ex.Message}");
        }
    }

    // Giữ lại hàm cũ để tránh lỗi compile nếu có chỗ khác dùng, nhưng redirect sang hàm mới
    public async Task<bool> DeleteMaterialAsync(int materialId)
    {
        var result = await DeleteMaterialDetailedAsync(materialId);
        return result.Success;
    }

    // BOM Implementations
    public async Task<List<Bom>> GetBomByParentCodeAsync(string parentCode)
    {
        var parent = await GetMaterialByCodeAsync(parentCode);
        if (parent == null) return new List<Bom>();

        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Boms
            .AsNoTracking()
            .Include(b => b.Child)
            .Where(b => b.ParentId == parent.MaterialId)
            .ToListAsync();
    }

    public async Task<bool> AddBomItemAsync(Bom bom)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            context.Boms.Add(bom);
            await context.SaveChangesAsync();
            var parent = await context.Materials.FindAsync(bom.ParentId);
            var child = await context.Materials.FindAsync(bom.ChildId);
            await _auditLogService.LogActionAsync("Cập nhật BOM", $"Thêm linh kiện {child?.MaterialCode} vào {parent?.MaterialCode}", "BOM");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteBomItemAsync(int bomId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var item = await context.Boms.FindAsync(bomId);
            if (item == null) return false;

            var parent = await context.Materials.FindAsync(item.ParentId);
            var child = await context.Materials.FindAsync(item.ChildId);
            context.Boms.Remove(item);
            await context.SaveChangesAsync();
            await _auditLogService.LogActionAsync("Xóa BOM", $"Gỡ {child?.MaterialCode} khỏi {parent?.MaterialCode}", "BOM");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Routing Implementations
    public async Task<List<Routing>> GetRoutingByParentCodeAsync(string parentCode)
    {
        var parent = await GetMaterialByCodeAsync(parentCode);
        if (parent == null) return new List<Routing>();

        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Routings
            .AsNoTracking()
            .Where(r => r.ProductId == parent.MaterialId)
            .OrderBy(r => r.StepNumber)
            .ToListAsync();
    }

    public async Task<bool> AddRoutingStepAsync(Routing step)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            context.Routings.Add(step);
            await context.SaveChangesAsync();
            var product = await context.Materials.FindAsync(step.ProductId);
            await _auditLogService.LogActionAsync("Cập nhật Quy trình", $"Thêm bước {step.StepNumber}: {step.StepName} cho {product?.MaterialCode}", "Routing");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteRoutingByProductIdAsync(int productId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var steps = await context.Routings.Where(r => r.ProductId == productId).ToListAsync();
            context.Routings.RemoveRange(steps);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<Bom>> GetAllBomsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Boms
            .AsNoTracking()
            .Include(b => b.Parent)
            .Include(b => b.Child)
            .ToListAsync();
    }

    public async Task<List<Routing>> GetAllRoutingsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Routings
            .AsNoTracking()
            .Include(r => r.Product)
            .OrderBy(r => r.ProductId)
            .ThenBy(r => r.StepNumber)
            .ToListAsync();
    }

    public async Task<bool> UpdateBomItemAsync(Bom bom)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            context.Boms.Update(bom);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateRoutingStepAsync(Routing step)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            context.Routings.Update(step);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> IsCircularReferenceAsync(int parentId, int childId)
    {
        if (parentId == childId) return true;

        using var context = await _contextFactory.CreateDbContextAsync();
        
        // We check if parentId exists anywhere in the sub-tree of childId
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(childId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == parentId) return true;
            if (visited.Contains(current)) continue;
            visited.Add(current);

            var children = await context.Boms
                .Where(b => b.ParentId == current)
                .Select(b => b.ChildId)
                .ToListAsync();

            foreach (var c in children)
            {
                if (c.HasValue) queue.Enqueue(c.Value);
            }
        }

        return false;
    }
}
