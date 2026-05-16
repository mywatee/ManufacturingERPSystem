using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Bom
{
    public int Bomid { get; set; }

    public int? ParentId { get; set; }

    public int? ChildId { get; set; }

    public decimal QuantityPerUnit { get; set; }

    public virtual Material? Child { get; set; }

    public virtual Material? Parent { get; set; }
}
