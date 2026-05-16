using System;

namespace ManufacturingERP.Models;

public class PayrollRecord
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int ProductionQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ProductionSalary => ProductionQty * UnitPrice;
    public decimal BasicSalary { get; set; }
    public decimal AttendanceBonus { get; set; }
    public decimal QualityBonus { get; set; }
    public decimal TotalSalary => BasicSalary + ProductionSalary + AttendanceBonus + QualityBonus;
    public string Status { get; set; } = "Chưa duyệt";
}

public class AttendanceSummary
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int WorkDays { get; set; }
    public int LateTimes { get; set; }
    public int AbsentDays { get; set; }
    public double OvertimeHours { get; set; }
    public string Evaluation { get; set; } = "Trung bình";
}

public class ProductivityStats
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public double NormalizedHeight { get; set; }
}
