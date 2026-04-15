using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Notification
{
    public int NotiId { get; set; }

    public int? RecipientId { get; set; }

    public int? RoleId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? Recipient { get; set; }

    public virtual Role? Role { get; set; }
}
