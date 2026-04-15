using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
