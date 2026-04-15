using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class EmployeeSchedule
{
    public int ScheduleId { get; set; }

    public int? UserId { get; set; }

    public int? ShiftId { get; set; }

    public DateOnly? WorkDate { get; set; }

    public string? MachineCode { get; set; }

    public virtual Shift? Shift { get; set; }

    public virtual User? User { get; set; }
}
