using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class WorkOrderProgress
{
    public int ProgressId { get; set; }

    public int? Woid { get; set; }

    public int? StepNumber { get; set; }

    public int? WorkerId { get; set; }

    public string? Status { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? MachineId { get; set; }

    public virtual WorkOrder? Wo { get; set; }

    public virtual User? Worker { get; set; }
}
