using System;

namespace ManufacturingERP.Models;

public partial class ActivityLog
{
    public int LogId { get; set; }

    public string ActivityType { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? PerformedBy { get; set; }

    public DateTime? Timestamp { get; set; }
}
