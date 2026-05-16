using System;
using System.Collections.Generic;
using System.Linq;

namespace ManufacturingERP.Models;

public partial class WorkOrderItem
{
    public int ItemId { get; set; }

    public int WorkOrderId { get; set; }

    public int? ProductId { get; set; }

    public int TargetQty { get; set; }

    public int ActualQty { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int InternalFailedQty => WorkOrderProgresses?
        .Where(p => p.StageName != "Kiểm tra chất lượng (QC)" && p.StageName != "CẢNH BÁO HỆ THỐNG")
        .Sum(p => p.DefectQty ?? 0) ?? 0;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int QCFailedQty => WorkOrderProgresses?
        .Where(p => p.StageName == "Kiểm tra chất lượng (QC)")
        .Sum(p => p.DefectQty ?? 0) ?? 0;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int TotalFailedQty => InternalFailedQty + QCFailedQty;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int ReportedQty => WorkOrderProgresses?
        .Where(p => p.StageName != "Kiểm tra chất lượng (QC)" && p.StageName != "CẢNH BÁO HỆ THỐNG")
        .Sum(p => p.ProducedQty ?? 0) ?? 0;





    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Material? Product { get; set; }

    public virtual WorkOrder WorkOrder { get; set; } = null!;

    public virtual ICollection<QualityControl> QualityControls { get; set; } = new List<QualityControl>();

    public virtual ICollection<WorkOrderProgress> WorkOrderProgresses { get; set; } = new List<WorkOrderProgress>();
}
