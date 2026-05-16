using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Partner
{
    public int PartnerId { get; set; }

    public string PartnerCode { get; set; } = null!;

    public string PartnerName { get; set; } = null!;

    public string? PartnerType { get; set; } // Supplier, Customer, Both

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? TaxCode { get; set; }

    public string? Status { get; set; } = "Hoạt động";

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;
}
