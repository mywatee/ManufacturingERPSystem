using System;

namespace ManufacturingERP.Models;

public class InventoryItemDisplay
{
    public string Id { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public double CurrentQty { get; set; }
    public double MinStock { get; set; }
    public double MaxStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = "An toàn"; // An toàn, Cảnh báo tồn thấp, Hết hàng

    public double StockPercentage => MaxStock > 0 ? Math.Min((CurrentQty / MaxStock) * 100, 100) : 0;
    public decimal TotalValue => (decimal)CurrentQty * UnitPrice;
}

public class StockTransactionDisplay
{
    public string Id { get; set; } = string.Empty;
    public string TransactionDate { get; set; } = string.Empty;
    public string Type { get; set; } = "Nhập kho"; // Nhập kho, Xuất kho
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public string TransBy { get; set; } = string.Empty;
    public string? ReferenceDoc { get; set; }
    public string? Notes { get; set; }
}

public class WarehouseConfig
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public double Capacity { get; set; } // m2
    public double Used { get; set; }     // m2
    public string Status { get; set; } = "Hoạt động"; // Hoạt động, Bảo trì

    public double FillPercentage => Capacity > 0 ? Math.Min((Used / Capacity) * 100, 100) : 0;
}

public class StockAlertDisplay
{
    public string Id { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public double CurrentQty { get; set; }
    public double MinStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string AlertLevel { get; set; } = "Cảnh báo"; // Nguy cấp, Cảnh báo, Lưu ý
    public string SpecialStatus { get; set; } = string.Empty; // e.g., "Hết hàng", "Còn 2 ngày"
    
    public double ShortageQuantity => Math.Max(MinStock - CurrentQty, 0);
    public string ExpectedEndRange { get; set; } = string.Empty; // e.g., "Đã hết", "3 ngày"
    
    public double AlertPercentage => MinStock > 0 ? Math.Min((CurrentQty / MinStock) * 100, 100) : 0;
}
