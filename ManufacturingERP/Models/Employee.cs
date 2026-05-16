using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ManufacturingERP.Models;

public partial class Employee : ObservableObject
{
    [Key]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string EmployeeCode { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? Department { get; set; }

    [MaxLength(50)]
    public string? Position { get; set; }

    public DateTime? JoinDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? BasicSalary { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, OnLeave, Resigned

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PiecesRate { get; set; } = 15000;

    public int? ProductivityThreshold { get; set; } = 500;

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<EmployeeSchedule> Schedules { get; set; } = new List<EmployeeSchedule>();
    public virtual User? UserAccount { get; set; }
}
