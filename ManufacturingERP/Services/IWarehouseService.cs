using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IWarehouseService
{
    Task<List<WarehouseConfig>> GetWarehousesAsync();
    Task<List<InventoryItemDisplay>> GetInventoryAsync(string warehouseName = "Tất cả");
    Task<List<StockTransactionDisplay>> GetTransactionsAsync(string warehouseName = "Tất cả", int limit = 100);
    Task<List<StockAlertDisplay>> GetStockAlertsAsync();
    Task<bool> AddStockTransactionAsync(StockTransaction transaction);
    Task<bool> AddWarehouseAsync(Warehouse warehouse);
    Task<bool> UpdateWarehouseAsync(Warehouse warehouse);
    Task<bool> DeleteWarehouseAsync(int warehouseId);
    Task<List<Material>> GetAllMaterialsAsync();
    Task<bool> AddStockTransactionsAsync(List<StockTransaction> transactions);
    Task<bool> TransferStockAsync(int fromWarehouseId, int toWarehouseId, int materialId, decimal quantity, int? userId);
    Task<bool> AdjustStockAsync(int warehouseId, int materialId, decimal actualQuantity, int? userId, string notes);
    Task<bool> CancelTransactionAsync(int transactionId, int userId);
    Task<List<StockTransaction>> GetTransactionsByReferenceAsync(string referenceCode);
}


