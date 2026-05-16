using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Inventory
{
    public int MaterialId { get; set; }

    public decimal? CurrentStock { get; set; }

    public string? WarehouseLocation { get; set; }

    public DateTime? LastUpdated { get; set; }

    public int? WarehouseId { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual Warehouse? Warehouse { get; set; }
}
