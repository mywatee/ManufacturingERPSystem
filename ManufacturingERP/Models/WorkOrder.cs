using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class WorkOrder
{
    public int Woid { get; set; }

    public string Wocode { get; set; } = null!;

    public int? ProductId { get; set; }

    public int TargetQty { get; set; }

    public int? ActualQty { get; set; }

    public string? Status { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Material? Product { get; set; }

    public virtual ICollection<QualityControl> QualityControls { get; set; } = new List<QualityControl>();

    public virtual ICollection<WorkOrderProgress> WorkOrderProgresses { get; set; } = new List<WorkOrderProgress>();
}
