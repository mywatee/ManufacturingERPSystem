using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class QualityControl
{
    public int Qcid { get; set; }

    public int? Woid { get; set; }
    public int? WorkOrderItemId { get; set; }

    public int? StepNumber { get; set; }

    public int? PassedQty { get; set; }

    public int? FailedQty { get; set; }

    public string? DefectReason { get; set; }

    public int? InspectorId { get; set; }

    public DateTime? InspectionDate { get; set; }

    public virtual User? Inspector { get; set; }

    public virtual WorkOrder? Wo { get; set; }
    public virtual WorkOrderItem? WorkOrderItem { get; set; }
}
