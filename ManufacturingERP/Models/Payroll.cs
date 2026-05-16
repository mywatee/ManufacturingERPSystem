using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingERP.Models;

public class Payroll
{
    [Key]
    public int PayrollId { get; set; }

    public int EmployeeId { get; set; }
    
    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }

    public int Month { get; set; }
    public int Year { get; set; }

    public int ProductionQty { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasicSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AttendanceBonus { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal QualityBonus { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSalary { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Chưa duyệt"; // Chưa duyệt, Đã duyệt, Đã thanh toán

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
}
