using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IHRService
{
    Task<List<Employee>> GetEmployeesAsync();
    Task<Employee> AddEmployeeAsync(Employee employee);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<List<Attendance>> GetDailyAttendanceAsync(DateTime date);
    Task<bool> RecordAttendanceAsync(int employeeId, string status, string? note = null);
    Task<List<PayrollRecord>> CalculateMonthlyPayrollAsync(int month, int year);
    Task<List<ProductivityStats>> GetTopPerformersAsync(int month, int year);
    Task<List<AttendanceSummary>> GetAttendanceSummariesAsync(int month, int year);
    Task<List<AttendanceSummary>> GetAttendanceSummariesInRangeAsync(DateTime startDate, DateTime endDate);
    Task<bool> SavePayrollAsync(List<PayrollRecord> records, int month, int year);
    Task<List<PayrollRecord>> GetSavedPayrollAsync(int month, int year);
    Task<bool> DeleteEmployeeAsync(int employeeId);

    // Shift Management
    Task<List<Shift>> GetShiftsAsync();
    Task<Shift> AddShiftAsync(Shift shift);
    Task<bool> UpdateShiftAsync(Shift shift);
    Task<bool> DeleteShiftAsync(int shiftId);

    // Schedule Management
    Task<List<EmployeeSchedule>> GetSchedulesAsync(DateTime startDate, DateTime endDate);
    Task<bool> AssignScheduleAsync(EmployeeSchedule schedule);
    Task<bool> RemoveScheduleAsync(int scheduleId);
    Task<string> GetNextEmployeeCodeAsync();
    Task<string> GetNextShiftCodeAsync();
}
