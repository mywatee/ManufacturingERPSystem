using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string? Code { get; set; }

    public string? WarehouseName { get; set; }

    public string? Location { get; set; }

    public int? ManagerId { get; set; }

    public decimal? Capacity { get; set; }

    public string? Status { get; set; }
    public string? WarehouseType { get; set; }
    public string? Description { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? CapacityUnit { get; set; }
    public decimal? SafetyThreshold { get; set; }

    public virtual User? Manager { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}
