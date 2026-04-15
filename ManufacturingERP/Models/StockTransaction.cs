using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class StockTransaction
{
    public int TransactionId { get; set; }

    public int? MaterialId { get; set; }

    public string? Type { get; set; }

    public decimal? Quantity { get; set; }

    public string? ReferenceCode { get; set; }

    public int? TransBy { get; set; }

    public DateTime? TransDate { get; set; }

    public virtual Material? Material { get; set; }

    public virtual User? TransByNavigation { get; set; }
}
