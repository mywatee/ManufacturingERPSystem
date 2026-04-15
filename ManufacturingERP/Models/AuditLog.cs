using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string? Action { get; set; }

    public string? TableName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime? Timestamp { get; set; }

    public virtual User? User { get; set; }
}
