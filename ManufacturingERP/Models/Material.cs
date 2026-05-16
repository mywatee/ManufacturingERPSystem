using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Material
{
    public int MaterialId { get; set; }

    public string MaterialCode { get; set; } = null!;

    public string MaterialName { get; set; } = null!;

    public string? Unit { get; set; }

    public string? Category { get; set; }
    
    public string? Status { get; set; } = "Đang sử dụng";

    public int? MinStock { get; set; }
    
    public decimal? UnitPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Bom> BomChildren { get; set; } = new List<Bom>();

    public virtual ICollection<Bom> BomParents { get; set; } = new List<Bom>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<Routing> Routings { get; set; } = new List<Routing>();

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    public virtual ICollection<WorkOrderItem> WorkOrderItemNavigations { get; set; } = new List<WorkOrderItem>();
}
