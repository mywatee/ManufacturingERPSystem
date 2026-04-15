using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Routing
{
    public int RoutingId { get; set; }

    public int? ProductId { get; set; }

    public int? StepNumber { get; set; }

    public string? StepName { get; set; }

    public int? EstimatedTime { get; set; }

    public virtual Material? Product { get; set; }
}
