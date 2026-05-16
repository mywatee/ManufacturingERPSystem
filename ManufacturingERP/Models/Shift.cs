using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Shift
{
    public int ShiftId { get; set; }

    public string? ShiftCode { get; set; }

    public string? ShiftName { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public TimeOnly? BreakStartTime { get; set; }

    public TimeOnly? BreakEndTime { get; set; }

    public string? ColorHex { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new List<EmployeeSchedule>();
}

