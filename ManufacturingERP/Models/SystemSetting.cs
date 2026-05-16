using System;
using System.ComponentModel.DataAnnotations;

namespace ManufacturingERP.Models;

public partial class SystemSetting
{
    [Key]
    public int SettingId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SettingKey { get; set; } = null!;

    [Required]
    public string SettingValue { get; set; } = null!;

    [MaxLength(100)]
    public string? Description { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
