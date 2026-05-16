using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingERP.Models;

public class RolePermission
{
    [Key, Column(Order = 0)]
    public int RoleId { get; set; }

    [Key, Column(Order = 1)]
    [MaxLength(100)]
    public string ModuleKey { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public virtual Role Role { get; set; } = null!;
}

