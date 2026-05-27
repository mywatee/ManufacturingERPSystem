using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Services;

public class HRService : IHRService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IAuditLogService _auditLogService;

    public HRService(IDbContextFactory<ManufacturingContext> contextFactory, IAuditLogService auditLogService)
    {
        _contextFactory = contextFactory;
        _auditLogService = auditLogService;
    }

    public async Task<List<Employee>> GetEmployeesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Employees
            .OrderBy(e => e.FullName)
            .ToListAsync();
    }

    public async Task<Employee> AddEmployeeAsync(Employee employee)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        await _auditLogService.LogActionAsync("THÊM NHÂN VIÊN", $"Đã thêm hồ sơ nhân viên: {employee.FullName} ({employee.EmployeeCode})", "Employees");
        return employee;
    }

    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Employees.Update(employee);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("CẬP NHẬT NHÂN VIÊN", $"Cập nhật hồ sơ nhân viên: {employee.FullName} ({employee.EmployeeCode})", "Employees");
        return success;
    }

    public async Task<List<Attendance>> GetDailyAttendanceAsync(DateTime date)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var start = date.Date;
        var end = start.AddDays(1);
        return await context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.Date >= start && a.Date < end)
            .ToListAsync();
    }

    public async Task<bool> RecordAttendanceAsync(int employeeId, string status, string? note = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.Now;
        var start = now.Date;
        var end = start.AddDays(1);
        var record = await context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date >= start && a.Date < end);

        if (record == null)
        {
            record = new Attendance
            {
                EmployeeId = employeeId,
                Date = now.Date,
                CheckIn = status == "Vắng" ? null : now.TimeOfDay,
                Status = status,
                Note = note
            };
            context.Attendances.Add(record);
        }
        else
        {
            record.CheckOut = now.TimeOfDay;
            record.Status = status;
            if (note != null) record.Note = note;
        }

        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("CHẤM CÔNG", $"Đã ghi nhận chấm công cho NV ID {employeeId}: {status}", "Attendances");
        return success;
    }

    public async Task<List<PayrollRecord>> CalculateMonthlyPayrollAsync(int month, int year)
    {
        // 1. Check if saved payroll exists first
        var savedPayroll = await GetSavedPayrollAsync(month, year);
        if (savedPayroll.Any()) return savedPayroll;

        using var context = await _contextFactory.CreateDbContextAsync();
        var employees = await context.Employees
            .Include(e => e.UserAccount)
            .Where(e => e.Status == "Đang làm việc" || e.Status == "Active")
            .ToListAsync();

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var payroll = new List<PayrollRecord>();

        foreach (var emp in employees)
        {
            int producedQty = 0;
            if (emp.UserAccount != null)
            {
                producedQty = await context.WorkOrderProgresses
                    .Where(p => p.WorkerId == emp.UserAccount.UserId && 
                               p.EndTime >= startDate && 
                               p.EndTime <= endDate)
                    .SumAsync(p => p.ProducedQty ?? 0);
            }

            // Attendance Stats
            var attendances = await context.Attendances
                .Where(a => a.EmployeeId == emp.EmployeeId && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            int onTimeDays = attendances.Count(a => a.Status == "Đúng giờ");
            int lateDays = attendances.Count(a => a.Status == "Đi muộn");

            // Pieces rate from employee record or default
            decimal piecesRate = emp.PiecesRate ?? 15000;

            // Only calculate production-based pay for Production and Technical departments
            bool isProductionDept = emp.Department == "Sản xuất" || emp.Department == "Kỹ thuật";
            
            if (!isProductionDept)
            {
                producedQty = 0;
                piecesRate = 0;
            }

            // Diligence Bonus: Full attendance (>= 22 days) and no lates (Applies to all)
            decimal diligenceBonus = (onTimeDays >= 22 && lateDays == 0) ? 500000 : 0;
            
            // Quality/Productivity Bonus based on threshold (Only for production)
            int threshold = emp.ProductivityThreshold ?? 500;
            decimal productivityBonus = (isProductionDept && producedQty > threshold) ? 300000 : 0;

            var record = new PayrollRecord
            {
                EmployeeId = emp.EmployeeId,
                EmployeeCode = emp.EmployeeCode ?? "",
                Name = emp.FullName,
                Department = emp.Department ?? "N/A",
                ProductionQty = producedQty,
                UnitPrice = piecesRate,
                BasicSalary = emp.BasicSalary ?? 5000000,
                AttendanceBonus = diligenceBonus,
                QualityBonus = productivityBonus,
                Status = "Chưa duyệt"
            };

            payroll.Add(record);
        }

        return payroll;
    }

    public async Task<bool> SavePayrollAsync(List<PayrollRecord> records, int month, int year)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Remove existing for this month/year if any
            var existing = await context.Payrolls.Where(p => p.Month == month && p.Year == year).ToListAsync();
            if (existing.Any()) context.Payrolls.RemoveRange(existing);

            foreach (var rec in records)
            {
                var payroll = new Payroll
                {
                    EmployeeId = rec.EmployeeId,
                    Month = month,
                    Year = year,
                    ProductionQty = rec.ProductionQty,
                    UnitPrice = rec.UnitPrice,
                    BasicSalary = rec.BasicSalary,
                    AttendanceBonus = rec.AttendanceBonus,
                    QualityBonus = rec.QualityBonus,
                    TotalSalary = rec.TotalSalary,
                    Status = rec.Status,
                    CreatedAt = DateTime.Now,
                    ApprovedAt = rec.ApprovedAt,
                    ApprovedBy = rec.ApprovedBy
                };
                context.Payrolls.Add(payroll);
            }

            bool success = await context.SaveChangesAsync() > 0;
            if (success) await _auditLogService.LogActionAsync("LƯU BẢNG LƯƠNG", $"Đã lưu bảng lương tháng {month}/{year} cho {records.Count} nhân viên", "Payrolls");
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SavePayrollAsync error: {ex}");
            return false;
        }
    }

    public async Task<List<PayrollRecord>> GetSavedPayrollAsync(int month, int year)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var saved = await context.Payrolls
            .Include(p => p.Employee)
            .Where(p => p.Month == month && p.Year == year)
            .ToListAsync();

        return saved.Select(p => new PayrollRecord
        {
            EmployeeId = p.EmployeeId,
            EmployeeCode = p.Employee?.EmployeeCode ?? "",
            Name = p.Employee?.FullName ?? "N/A",
            Department = p.Employee?.Department ?? "N/A",
            ProductionQty = p.ProductionQty,
            UnitPrice = p.UnitPrice,
            BasicSalary = p.BasicSalary,
            AttendanceBonus = p.AttendanceBonus,
            QualityBonus = p.QualityBonus,
            Status = p.Status,
            ApprovedAt = p.ApprovedAt,
            ApprovedBy = p.ApprovedBy
        }).ToList();
    }

    public async Task<List<ProductivityStats>> GetTopPerformersAsync(int month, int year)
    {
        var payroll = await CalculateMonthlyPayrollAsync(month, year);
        var maxVal = payroll.Any() ? payroll.Max(p => p.ProductionQty) : 1;

        return payroll
            .OrderByDescending(p => p.ProductionQty)
            .Take(5)
            .Select(p => new ProductivityStats
            {
                Name = p.Name,
                Value = p.ProductionQty,
                NormalizedHeight = (double)p.ProductionQty / maxVal
            })
            .ToList();
    }

    public async Task<List<AttendanceSummary>> GetAttendanceSummariesAsync(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return await GetAttendanceSummariesInRangeAsync(startDate, endDate);
    }

    public async Task<List<AttendanceSummary>> GetAttendanceSummariesInRangeAsync(DateTime startDate, DateTime endDate)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var employees = await context.Employees.ToListAsync();
        
        var summaries = new List<AttendanceSummary>();
        
        foreach (var emp in employees)
        {
            var attendances = await context.Attendances
                .Where(a => a.EmployeeId == emp.EmployeeId && a.Date >= startDate.Date && a.Date <= endDate.Date)
                .ToListAsync();
                
            int workDays = attendances.Count(a => a.Status == "Đúng giờ" || a.Status == "Đi muộn");
            int lateTimes = attendances.Count(a => a.Status == "Đi muộn");
            
            double otHours = 0;
            foreach(var a in attendances)
            {
                if (a.CheckIn != null && a.CheckOut != null)
                {
                    var duration = a.CheckOut.Value - a.CheckIn.Value;
                    var ot = duration.TotalHours - 8;
                    if (ot > 0) otHours += ot;
                }
            }
            
            string evaluation = "Trung bình";
            if (workDays >= 20 && lateTimes == 0) evaluation = "Xuất sắc";
            else if (workDays >= 15) evaluation = "Tốt";
            
            summaries.Add(new AttendanceSummary
            {
                EmployeeId = emp.EmployeeId,
                EmployeeCode = emp.EmployeeCode,
                Name = emp.FullName,
                Department = emp.Department,
                WorkDays = workDays,
                LateTimes = lateTimes,
                AbsentDays = (int)(endDate.Date - startDate.Date).TotalDays + 1 - workDays, // Simple estimation
                OvertimeHours = Math.Round(otHours, 1),
                Evaluation = evaluation
            });
        }
        
        return summaries;
    }

    public async Task<bool> DeleteEmployeeAsync(int employeeId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var employee = await context.Employees
            .Include(e => e.UserAccount)
            .Include(e => e.Attendances)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null) return false;

        try 
        {
            // 1. Delete associated attendances
            if (employee.Attendances.Any())
            {
                context.Attendances.RemoveRange(employee.Attendances);
            }

            // 2. Handle associated user account and schedules
            if (employee.UserAccount != null)
            {
                var userId = employee.UserAccount.UserId;
                
                // Check if user has critical history that MUST NOT be deleted (Audit/Traceability)
                bool hasCriticalHistory = await context.WorkOrders.AnyAsync(w => w.CreatedBy == userId) ||
                                         await context.StockTransactions.AnyAsync(s => s.TransBy == userId) ||
                                         await context.QualityControls.AnyAsync(q => q.InspectorId == userId) ||
                                         await context.WorkOrderProgresses.AnyAsync(wp => wp.WorkerId == userId);

                if (hasCriticalHistory)
                {
                    return false; // Blocks deletion for data integrity
                }

                // 2.1. Cleanup MANY-TO-MANY relationships (Roles)
                var userWithRoles = await context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.UserId == userId);
                if (userWithRoles != null)
                {
                    userWithRoles.Roles.Clear();
                    await context.SaveChangesAsync(); // Update the join table
                }

                // 2.2. Cleanup transient data (Schedules, Notifications, Reset Requests)
                var schedules = await context.EmployeeSchedules.Where(s => s.UserId == userId).ToListAsync();
                if (schedules.Any()) context.EmployeeSchedules.RemoveRange(schedules);

                var notifications = await context.Notifications.Where(n => n.RecipientId == userId).ToListAsync();
                if (notifications.Any()) context.Notifications.RemoveRange(notifications);

                var resetRequests = await context.PasswordResetRequests.Where(r => r.UserId == userId).ToListAsync();
                if (resetRequests.Any()) context.PasswordResetRequests.RemoveRange(resetRequests);
                
                // 2.3. Actually remove the user
                context.Users.Remove(employee.UserAccount);
            }

            // 3. Delete employee
            context.Employees.Remove(employee);
            
            bool success = await context.SaveChangesAsync() > 0;
            if (success) await _auditLogService.LogActionAsync("XÓA NHÂN VIÊN", $"Đã xóa hồ sơ nhân viên ID {employeeId}", "Employees");
            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting employee: {ex.Message}");
            return false;
        }
    }

    public async Task<List<Shift>> GetShiftsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Shifts.ToListAsync();
    }

    public async Task<Shift> AddShiftAsync(Shift shift)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Shifts.Add(shift);
        await context.SaveChangesAsync();
        await _auditLogService.LogActionAsync("TẠO CA LÀM", $"Đã tạo ca làm việc mới: {shift.ShiftName} ({shift.ShiftCode})", "Shifts");
        return shift;
    }

    public async Task<bool> UpdateShiftAsync(Shift shift)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Shifts.Update(shift);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("SỬA CA LÀM", $"Đã cập nhật ca làm việc: {shift.ShiftName} ({shift.ShiftCode})", "Shifts");
        return success;
    }

    public async Task<bool> DeleteShiftAsync(int shiftId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var shift = await context.Shifts.FindAsync(shiftId);
        if (shift == null) return false;
        context.Shifts.Remove(shift);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("XÓA CA LÀM", $"Đã xóa ca làm việc ID {shiftId}", "Shifts");
        return success;
    }

    public async Task<List<EmployeeSchedule>> GetSchedulesAsync(DateTime startDate, DateTime endDate)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var start = DateOnly.FromDateTime(startDate);
        var end = DateOnly.FromDateTime(endDate);

        return await context.EmployeeSchedules
            .Include(s => s.Shift)
            .Include(s => s.User)
                .ThenInclude(u => u.Employee)
            .Where(s => s.WorkDate >= start && s.WorkDate <= end)
            .ToListAsync();
    }

    public async Task<bool> AssignScheduleAsync(EmployeeSchedule schedule)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check for existing schedule for this user on this date
        var existing = await context.EmployeeSchedules
            .FirstOrDefaultAsync(s => s.UserId == schedule.UserId && s.WorkDate == schedule.WorkDate);

        if (existing != null)
        {
            existing.ShiftId = schedule.ShiftId;
            existing.MachineCode = schedule.MachineCode;
            context.EmployeeSchedules.Update(existing);
        }
        else
        {
            context.EmployeeSchedules.Add(schedule);
        }

        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("PHÂN CA", $"Đã phân/cập nhật lịch trực ngày {schedule.WorkDate} cho người dùng ID {schedule.UserId}", "EmployeeSchedules");
        return success;
    }

    public async Task<bool> RemoveScheduleAsync(int scheduleId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var schedule = await context.EmployeeSchedules.FindAsync(scheduleId);
        if (schedule == null) return false;
        context.EmployeeSchedules.Remove(schedule);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("HỦY CA", $"Đã hủy lịch trực ID {scheduleId} ngày {schedule.WorkDate}", "EmployeeSchedules");
        return success;
    }

    public async Task<string> GetNextEmployeeCodeAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var maxCode = await context.Employees
            .Where(e => e.EmployeeCode.StartsWith("EMP") && e.EmployeeCode.Length == 6)
            .Select(e => e.EmployeeCode)
            .OrderByDescending(c => c)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(maxCode)) return "EMP001";

        // Extract number part
        string numericPart = maxCode.Substring(3);
        if (int.TryParse(numericPart, out int number))
        {
            return $"EMP{(number + 1):D3}";
        }

        // Fallback if format is weird
        return "EMP001";
    }

    public async Task<string> GetNextShiftCodeAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var maxCode = await context.Shifts
            .Where(s => s.ShiftCode != null && s.ShiftCode.StartsWith("SH"))
            .Select(s => s.ShiftCode)
            .OrderByDescending(c => c)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(maxCode)) return "SH01";

        // Try to parse the numeric part after 'SH'
        string numericPart = new string(maxCode.Skip(2).Where(char.IsDigit).ToArray());
        if (int.TryParse(numericPart, out int lastNumber))
        {
            return $"SH{(lastNumber + 1):D2}";
        }

        return "SH01";
    }
}
