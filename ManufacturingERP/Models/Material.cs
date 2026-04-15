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

    public int? MinStock { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Bom> BomChildren { get; set; } = new List<Bom>();

    public virtual ICollection<Bom> BomParents { get; set; } = new List<Bom>();

    public virtual Inventory? Inventory { get; set; }

    public virtual ICollection<Routing> Routings { get; set; } = new List<Routing>();

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
