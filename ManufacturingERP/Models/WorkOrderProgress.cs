using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class WorkOrderProgress
{
    public int ProgressId { get; set; }

    public int? Woid { get; set; }
    public int? WorkOrderItemId { get; set; }

    public int? StepNumber { get; set; }

    public int? WorkerId { get; set; }

    public string? Status { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? MachineId { get; set; }

    // Giai đoạn 2: Bổ sung các trường để lưu chi tiết tiến độ
    public int? ProducedQty { get; set; }
    public int? DefectQty { get; set; }
    public string? StageName { get; set; }
    public string? RecordedBy { get; set; }
    public string? Notes { get; set; }

    public virtual WorkOrder? Wo { get; set; }
    public virtual WorkOrderItem? WorkOrderItem { get; set; }
    public virtual User? Worker { get; set; }
}
