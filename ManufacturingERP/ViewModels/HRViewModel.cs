using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Core;
using System.Data;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;

public partial class EmployeeAttendanceItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public Employee Employee { get; set; } = null!;
    public Attendance? Record { get; set; }
    
    public Shift? ScheduledShift { get; set; }
    
    public string ShiftDisplay 
    {
        get
        {
            if (ScheduledShift == null) return "Chưa phân ca";
            var hours = (ScheduledShift.StartTime.HasValue && ScheduledShift.EndTime.HasValue)
                ? $" ({ScheduledShift.StartTime.Value:HH:mm} - {ScheduledShift.EndTime.Value:HH:mm})"
                : "";
            return $"{ScheduledShift.ShiftName}{hours}";
        }
    }
    
    public string TimingStatus 
    {
        get
        {
            if (IsAbsent) return "VẮNG MẶT";
            
            if (IsCheckedIn)
            {
                if (Record?.CheckIn == null || ScheduledShift?.StartTime == null) return "ĐÃ ĐIỂM DANH";
                
                var checkIn = Record.CheckIn.Value;
                var startTime = ScheduledShift.StartTime.Value;
                var checkInTimeOnly = new TimeOnly(checkIn.Ticks);
                
                if (checkInTimeOnly > startTime.AddMinutes(15))
                    return "ĐI MUỘN";
                
                return "ĐÚNG GIỜ";
            }

            // Not checked in yet
            if (ScheduledShift != null && ScheduledShift.StartTime.HasValue && ScheduledShift.EndTime.HasValue)
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);

                // Night shift crossing midnight (e.g., 22:00-06:00)
                if (ScheduledShift.StartTime.Value > ScheduledShift.EndTime.Value)
                {
                    if (now <= ScheduledShift.EndTime.Value || now >= ScheduledShift.StartTime.Value)
                        return "CHƯA VÀO CA";
                    return "VẮNG MẶT";
                }

                if (now < ScheduledShift.StartTime.Value)
                    return "CHƯA ĐẾN GIỜ";
                if (now > ScheduledShift.EndTime.Value)
                    return "VẮNG MẶT"; // Missed the whole shift
            }

            return "CHƯA VÀO CA";
        }
    }

    public string TimingStatusColor => TimingStatus switch
    {
        "ĐÚNG GIỜ" => "#16A34A",
        "ĐI MUỘN" => "#DC2626",
        "VẮNG MẶT" => "#EF4444",
        _ => "#64748B"
    };

    public string StatusDisplay 
    {
        get 
        {
            if (Record == null) return "Chưa vào ca";
            if (Record.Status == "Vắng") return "Vắng mặt";
            if (IsCheckedOut) return "Đã ra ca";
            return Record.Status ?? "Đã vào ca";
        }
    }
    
    public string CheckInDisplay => Record?.CheckIn?.ToString(@"hh\:mm") ?? "--:--";
    public string CheckOutDisplay => Record?.CheckOut?.ToString(@"hh\:mm") ?? "--:--";
    public bool IsCheckedIn => Record != null && Record.Status != "Vắng";
    public bool IsAbsent => Record != null && Record.Status == "Vắng";
    public bool IsCheckedOut => Record?.CheckOut != null;
    public bool CanCheckOut => IsCheckedIn && !IsCheckedOut;
}

public partial class HRViewModel : ViewModelBase
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IAccessControlService _accessControlService;
    private readonly INotificationService _notificationService;
    private readonly IHRService _hrService;
    private readonly IUserManagementService _userService;
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;
    private readonly IFileService _fileService;

    [ObservableProperty] private ObservableCollection<Employee> _employeeProfiles = new();
    [ObservableProperty] private ObservableCollection<Employee> _allEmployeesObservable = new();
    [ObservableProperty] private ObservableCollection<PayrollRecord> _payrollRecords = new();
    [ObservableProperty] private ObservableCollection<Attendance> _attendanceRecords = new();
    [ObservableProperty] private ObservableCollection<EmployeeAttendanceItem> _dailyAttendanceList = new();
    [ObservableProperty] private ObservableCollection<AttendanceSummary> _attendanceSummaries = new();
    [ObservableProperty] private ObservableCollection<ProductivityStats> _productivityChart = new();
    [ObservableProperty] private ObservableCollection<ProductivityStats> _topPerformers = new();
    
    [ObservableProperty] private bool _isProfilesTabActive = true;

    [ObservableProperty] private bool _canAddHR;
    [ObservableProperty] private bool _canEditHR;
    [ObservableProperty] private bool _canDeleteHR;
    [ObservableProperty] private bool _isPayrollTabActive;
    [ObservableProperty] private bool _isAttendanceTabActive;
    [ObservableProperty] private bool _isSchedulesTabActive;
    [ObservableProperty] private bool _isReportsTabActive;

    // Report Configurations
    [ObservableProperty] private bool _isEmployeeReportSelected = true;
    [ObservableProperty] private bool _isPayrollReportSelected;
    [ObservableProperty] private bool _isAttendanceReportSelected;
    [ObservableProperty] private bool _isScheduleReportSelected;
    [ObservableProperty] private DateTime _reportStartDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime _reportEndDate = DateTime.Today;
    [ObservableProperty] private string _selectedReportDepartment = "Tất cả";
    [ObservableProperty] private bool _isExcelReport = true;
    [ObservableProperty] private bool _isPdfReport;
    [ObservableProperty] private ObservableCollection<ExportColumn> _reportColumns = new();

    partial void OnIsEmployeeReportSelectedChanged(bool value) { if (value) { IsPayrollReportSelected = false; IsAttendanceReportSelected = false; IsScheduleReportSelected = false; UpdateReportColumns(); } }
    partial void OnIsPayrollReportSelectedChanged(bool value) { if (value) { IsEmployeeReportSelected = false; IsAttendanceReportSelected = false; IsScheduleReportSelected = false; UpdateReportColumns(); } }
    partial void OnIsAttendanceReportSelectedChanged(bool value) { if (value) { IsEmployeeReportSelected = false; IsPayrollReportSelected = false; IsScheduleReportSelected = false; UpdateReportColumns(); } }
    partial void OnIsScheduleReportSelectedChanged(bool value) { if (value) { IsEmployeeReportSelected = false; IsPayrollReportSelected = false; IsAttendanceReportSelected = false; UpdateReportColumns(); } }

    private void UpdateReportColumns()
    {
        ReportColumns.Clear();
        if (IsEmployeeReportSelected)
        {
            ReportColumns.Add(new ExportColumn { Name = "Mã NV" });
            ReportColumns.Add(new ExportColumn { Name = "Họ tên" });
            ReportColumns.Add(new ExportColumn { Name = "Phòng ban" });
            ReportColumns.Add(new ExportColumn { Name = "Chức vụ" });
            ReportColumns.Add(new ExportColumn { Name = "SĐT" });
            ReportColumns.Add(new ExportColumn { Name = "Email" });
            ReportColumns.Add(new ExportColumn { Name = "Ngày vào làm" });
        }
        else if (IsPayrollReportSelected)
        {
            ReportColumns.Add(new ExportColumn { Name = "Tháng/Năm" });
            ReportColumns.Add(new ExportColumn { Name = "Mã NV" });
            ReportColumns.Add(new ExportColumn { Name = "Họ tên" });
            ReportColumns.Add(new ExportColumn { Name = "Lương cơ bản" });
            ReportColumns.Add(new ExportColumn { Name = "Lương sản phẩm" });
            ReportColumns.Add(new ExportColumn { Name = "Phụ cấp" });
            ReportColumns.Add(new ExportColumn { Name = "Khấu trừ" });
            ReportColumns.Add(new ExportColumn { Name = "Thực lĩnh" });
        }
        else if (IsAttendanceReportSelected)
        {
            ReportColumns.Add(new ExportColumn { Name = "Mã NV" });
            ReportColumns.Add(new ExportColumn { Name = "Họ tên" });
            ReportColumns.Add(new ExportColumn { Name = "Tổng ngày công" });
            ReportColumns.Add(new ExportColumn { Name = "Tổng tăng ca" });
            ReportColumns.Add(new ExportColumn { Name = "Số lần đi muộn" });
            ReportColumns.Add(new ExportColumn { Name = "Số ngày nghỉ" });
        }
        else if (IsScheduleReportSelected)
        {
            ReportColumns.Add(new ExportColumn { Name = "Mã NV" });
            ReportColumns.Add(new ExportColumn { Name = "Họ tên" });
            ReportColumns.Add(new ExportColumn { Name = "Ngày" });
            ReportColumns.Add(new ExportColumn { Name = "Ca làm việc" });
            ReportColumns.Add(new ExportColumn { Name = "Bắt đầu" });
            ReportColumns.Add(new ExportColumn { Name = "Kết thúc" });
        }
    }

    // Stats
    [ObservableProperty] private int _totalEmployees;
    [ObservableProperty] private int _activeEmployees;
    [ObservableProperty] private int _onLeaveEmployees;
    [ObservableProperty] private ObservableCollection<Shift> _shifts = new();
    [ObservableProperty] private ObservableCollection<EmployeeSchedule> _employeeSchedules = new();
    [ObservableProperty] private Shift? _selectedShift;

    [ObservableProperty] private string _totalSalaryText = "0 đ";
    [ObservableProperty] private int _totalProductionQty;
    [ObservableProperty] private int _approvedCount;
    [ObservableProperty] private int _waitingCount;

    [ObservableProperty] private int _presentCount;
    [ObservableProperty] private int _absentCount;
    [ObservableProperty] private int _lateCount;
    [ObservableProperty] private double _totalOvertimeHours;
    [ObservableProperty] private string _currentMonthText = string.Empty;
    [ObservableProperty] private string _todayText = string.Empty;
    [ObservableProperty] private int _notCheckedInCount;
    [ObservableProperty] private string _notCheckedInMessage = "Tất cả nhân sự đã điểm danh";

    // Attendance Sheet (Matrix View)
    [ObservableProperty] private DataTable _attendanceSheetData = new();
    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddDays(-6);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;
    [ObservableProperty] private string _viewMode = "Tuần"; // Tuần, Tháng, Tùy chọn
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private int _totalEmployeesCount;
    [ObservableProperty] private int _totalPagesCount;
    
    // Attendance Summary Pagination
    [ObservableProperty] private int _summaryCurrentPage = 1;
    [ObservableProperty] private int _summaryTotalPages = 1;
    [ObservableProperty] private int _summaryTotalCount = 0;
    private const int SummaryPageSize = 20;

    // Schedule Matrix (Matrix View)
    [ObservableProperty] private DataTable _scheduleMatrixData = new();
    [ObservableProperty] private int _scheduleMonth = DateTime.Today.Month;
    [ObservableProperty] private int _scheduleYear = DateTime.Today.Year;
    [ObservableProperty] private ObservableCollection<int> _availableMonths = new(Enumerable.Range(1, 12));
    [ObservableProperty] private ObservableCollection<int> _availableYears = new(Enumerable.Range(2024, 5));
    
    [ObservableProperty] private int _scheduleCurrentPage = 1;
    [ObservableProperty] private int _schedulePageSize = 10;
    [ObservableProperty] private int _scheduleTotalPages;
    [ObservableProperty] private int _scheduleTotalEmployees;
    
    // Legend/Guide for schedule matrix
    public string ScheduleGuide => "💡 Hướng dẫn: Chuột phải vào ô ca làm việc (VD: Ca1, Ca2) để Sửa hoặc Hủy lịch trực của ngày đó.";

    partial void OnScheduleMonthChanged(int value) { ScheduleCurrentPage = 1; _ = GenerateScheduleMatrix(); }
    partial void OnScheduleYearChanged(int value) { ScheduleCurrentPage = 1; _ = GenerateScheduleMatrix(); }
    partial void OnScheduleCurrentPageChanged(int value) => _ = GenerateScheduleMatrix();

    // Matrix View Filters
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedDepartmentFilter = "Tất cả";

    // Summary Table Filters (Manual properties to bypass generator issues)
    private string _summarySearchText = string.Empty;
    public string SummarySearchText
    {
        get => _summarySearchText;
        set { if (SetProperty(ref _summarySearchText, value)) LoadSummaryDataAsync(); }
    }

    private string _summarySelectedDepartmentFilter = "Tất cả";
    public string SummarySelectedDepartmentFilter
    {
        get => _summarySelectedDepartmentFilter;
        set { if (SetProperty(ref _summarySelectedDepartmentFilter, value)) LoadSummaryDataAsync(); }
    }

    private string _summaryViewMode = "Tháng";
    public string SummaryViewMode
    {
        get => _summaryViewMode;
        set 
        { 
            if (SetProperty(ref _summaryViewMode, value))
            {
                UpdateSummaryDateRange();
                LoadSummaryDataAsync();
            }
        }
    }

    private DateTime _summaryStartDate = DateTime.Today.AddDays(-7);
    public DateTime SummaryStartDate
    {
        get => _summaryStartDate;
        set { if (SetProperty(ref _summaryStartDate, value)) LoadSummaryDataAsync(); }
    }

    private DateTime _summaryEndDate = DateTime.Today;
    public DateTime SummaryEndDate
    {
        get => _summaryEndDate;
        set { if (SetProperty(ref _summaryEndDate, value)) LoadSummaryDataAsync(); }
    }

    private DateTime _summaryMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime SummaryMonth
    {
        get => _summaryMonth;
        set 
        { 
            if (SetProperty(ref _summaryMonth, value))
            {
                SummaryMonthText = $"Tháng {value:MM, yyyy}";
                UpdateSummaryDateRange();
                LoadSummaryDataAsync();
            }
        }
    }

    private string _summaryMonthText = string.Empty;
    public string SummaryMonthText
    {
        get => _summaryMonthText;
        set => SetProperty(ref _summaryMonthText, value);
    }

    public ObservableCollection<string> DepartmentFilters { get; } = new() { "Tất cả", "Sản xuất", "Kho bãi", "Kỹ thuật", "Hành chính", "Kế toán" };
    private List<Employee> _allEmployees = new();

    partial void OnSearchTextChanged(string value) 
    {
        ApplyFilters();
        _ = GenerateAttendanceSheet();
        _ = GenerateScheduleMatrix();
    }
    
    partial void OnSelectedDepartmentFilterChanged(string value) 
    {
        ApplyFilters();
        _ = GenerateAttendanceSheet();
        _ = GenerateScheduleMatrix();
    }

    [RelayCommand]
    private void NextMonth() => SummaryMonth = SummaryMonth.AddMonths(1);

    [RelayCommand]
    private void PrevMonth() => SummaryMonth = SummaryMonth.AddMonths(-1);

    [RelayCommand]
    private void SetSummaryViewMode(string mode) => SummaryViewMode = mode;

    private void UpdateSummaryDateRange()
    {
        switch (SummaryViewMode)
        {
            case "Tuần":
                _summaryStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
                _summaryEndDate = _summaryStartDate.AddDays(6);
                OnPropertyChanged(nameof(SummaryStartDate));
                OnPropertyChanged(nameof(SummaryEndDate));
                break;
            case "Tháng":
                _summaryStartDate = new DateTime(SummaryMonth.Year, SummaryMonth.Month, 1);
                _summaryEndDate = _summaryStartDate.AddMonths(1).AddDays(-1);
                OnPropertyChanged(nameof(SummaryStartDate));
                OnPropertyChanged(nameof(SummaryEndDate));
                break;
        }
    }

    [RelayCommand]
    private async Task LoadSummaryData()
    {
        SummaryCurrentPage = 1;
        await LoadSummaryDataAsync();
    }

    [RelayCommand]
    private async Task ExportHRReport()
    {
        if (IsBusy) return;

        if (ReportStartDate > ReportEndDate)
        {
            _notificationService.ShowError("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            return;
        }

        var selectedCols = ReportColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
        if (!selectedCols.Any())
        {
            _notificationService.ShowWarning("Vui lòng chọn ít nhất một thông tin hiển thị để xuất báo cáo.");
            return;
        }

        string reportType = IsEmployeeReportSelected ? "Hồ sơ nhân sự" :
                            IsPayrollReportSelected ? "Bảng lương" :
                            IsAttendanceReportSelected ? "Chấm công" : "Lịch làm việc";

        string prefix = IsEmployeeReportSelected ? "Ho_so_nhan_su" :
                        IsPayrollReportSelected ? "Bang_luong" :
                        IsAttendanceReportSelected ? "Cham_cong" : "Lich_lam_viec";

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"Bao_cao_{prefix}_{DateTime.Now:yyyyMMdd}",
            DefaultExt = IsExcelReport ? ".xlsx" : ".pdf",
            Filter = IsExcelReport ? "Excel Files|*.xlsx" : "PDF Files|*.pdf"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        try
        {
            bool success = false;
            using var context = _contextFactory.CreateDbContext();

            if (IsEmployeeReportSelected)
            {
                var data = await context.Employees
                    .Where(x => x.JoinDate >= ReportStartDate && x.JoinDate <= ReportEndDate)
                    .ToListAsync();
                
                if (SelectedReportDepartment != "Tất cả")
                    data = data.Where(x => x.Department == SelectedReportDepartment).ToList();
                
                if (!data.Any()) { _notificationService.ShowWarning("Không có dữ liệu trong khoảng thời gian này."); return; }

                success = IsExcelReport 
                    ? await _fileService.ExportToExcelAsync(data, dialog.FileName, "Hồ sơ", selectedCols, reportType)
                    : await _fileService.ExportToPdfAsync(data, dialog.FileName, reportType, selectedCols);
            }
            else if (IsPayrollReportSelected)
            {
                var empLookup = await context.Employees.ToDictionaryAsync(e => e.EmployeeId, e => e.EmployeeCode);
                var pData = new List<FlatPayrollExport>();
                
                var startMonth = new DateTime(ReportStartDate.Year, ReportStartDate.Month, 1);
                var endMonth = new DateTime(ReportEndDate.Year, ReportEndDate.Month, 1);
                
                for (var m = startMonth; m <= endMonth; m = m.AddMonths(1))
                {
                    var monthlyPayroll = await _hrService.CalculateMonthlyPayrollAsync(m.Month, m.Year);
                    if (SelectedReportDepartment != "Tất cả")
                        monthlyPayroll = monthlyPayroll.Where(p => p.Department == SelectedReportDepartment).ToList();
                    
                    pData.AddRange(monthlyPayroll.Select(p => new FlatPayrollExport
                    {
                        MonthYear = $"{m.Month:D2}/{m.Year}",
                        EmployeeCode = empLookup.ContainsKey(p.EmployeeId) ? empLookup[p.EmployeeId] : "",
                        Name = p.Name,
                        BasicSalary = p.BasicSalary,
                        ProductionSalary = p.ProductionSalary,
                        Allowance = p.AttendanceBonus + p.QualityBonus,
                        Deduction = 0, // Simplified deduction mock
                        TotalSalary = p.TotalSalary
                    }));
                }

                if (!pData.Any()) { _notificationService.ShowWarning("Không có dữ liệu trong khoảng thời gian này."); return; }

                success = IsExcelReport 
                    ? await _fileService.ExportToExcelAsync(pData, dialog.FileName, "Lương", selectedCols, reportType)
                    : await _fileService.ExportToPdfAsync(pData, dialog.FileName, reportType, selectedCols);
            }
            else if (IsAttendanceReportSelected)
            {
                var attendanceQuery = context.Attendances
                    .Include(a => a.Employee)
                    .Where(a => a.Date >= ReportStartDate && a.Date <= ReportEndDate);

                if (SelectedReportDepartment != "Tất cả")
                    attendanceQuery = attendanceQuery.Where(a => a.Employee.Department == SelectedReportDepartment);

                var rawAttendance = await attendanceQuery.ToListAsync();
                var aData = rawAttendance.GroupBy(a => a.EmployeeId).Select(g => new AttendanceSummary {
                    EmployeeId = g.Key,
                    EmployeeCode = g.First().Employee?.EmployeeCode ?? "",
                    Name = g.First().Employee?.FullName ?? "",
                    Department = g.First().Employee?.Department ?? "",
                    WorkDays = g.Count(x => x.Status == "Đúng giờ" || x.Status == "Đi muộn"),
                    LateTimes = g.Count(x => x.Status == "Đi muộn"),
                    AbsentDays = g.Count(x => x.Status == "Nghỉ phép" || x.Status == "Vắng"),
                    OvertimeHours = g.Sum(x => x.OvertimeHours)
                }).ToList();

                if (!aData.Any()) { _notificationService.ShowWarning("Không có dữ liệu trong khoảng thời gian này."); return; }

                success = IsExcelReport 
                    ? await _fileService.ExportToExcelAsync(aData, dialog.FileName, "Chấm công", selectedCols, reportType)
                    : await _fileService.ExportToPdfAsync(aData, dialog.FileName, reportType, selectedCols);
            }
            else if (IsScheduleReportSelected)
            {
                var startDateOnly = DateOnly.FromDateTime(ReportStartDate);
                var endDateOnly = DateOnly.FromDateTime(ReportEndDate);

                var schedulesQuery = context.EmployeeSchedules
                    .Include(s => s.User).ThenInclude(u => u.Employee)
                    .Include(s => s.Shift)
                    .Where(s => s.WorkDate >= startDateOnly && s.WorkDate <= endDateOnly);

                if (SelectedReportDepartment != "Tất cả")
                    schedulesQuery = schedulesQuery.Where(s => s.User.Employee.Department == SelectedReportDepartment);

                var schedules = await schedulesQuery.ToListAsync();
                var sData = schedules.Select(s => new {
                    EmployeeCode = s.User?.Employee?.EmployeeCode ?? "",
                    FullName = s.User?.Employee?.FullName ?? s.User?.Username ?? "",
                    WorkDate = s.WorkDate,
                    ShiftName = s.Shift?.ShiftName ?? "",
                    StartTime = s.Shift?.StartTime,
                    EndTime = s.Shift?.EndTime
                }).ToList();

                if (!sData.Any()) { _notificationService.ShowWarning("Không có dữ liệu trong khoảng thời gian này."); return; }

                success = IsExcelReport 
                    ? await _fileService.ExportToExcelAsync(sData, dialog.FileName, "Lịch", selectedCols, reportType)
                    : await _fileService.ExportToPdfAsync(sData, dialog.FileName, reportType, selectedCols);
            }

            if (success)
            {
                _notificationService.ShowSuccess($"Đã trích xuất báo cáo {reportType} thành công!\nĐã lưu tại: {dialog.FileName}");
            }
            else
            {
                _notificationService.ShowError("Có lỗi xảy ra trong quá trình tạo tệp báo cáo.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi khi xuất báo cáo: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PrevSummaryPage()
    {
        if (SummaryCurrentPage > 1)
        {
            SummaryCurrentPage--;
            await LoadSummaryDataAsync();
        }
    }

    [RelayCommand]
    private async Task NextSummaryPage()
    {
        if (SummaryCurrentPage < SummaryTotalPages)
        {
            SummaryCurrentPage++;
            await LoadSummaryDataAsync();
        }
    }

    private async Task LoadSummaryDataAsync()
    {
        IsBusy = true;
        try
        {
            using var context = _contextFactory.CreateDbContext();
            
            // 1. Get filtered employees
            var query = context.Employees.Include(e => e.UserAccount).AsQueryable();
            
            if (!string.IsNullOrWhiteSpace(SummarySearchText))
            {
                query = query.Where(e => e.FullName.Contains(SummarySearchText) || e.EmployeeCode.Contains(SummarySearchText));
            }
            
            if (SummarySelectedDepartmentFilter != "Tất cả")
            {
                query = query.Where(e => e.Department == SummarySelectedDepartmentFilter);
            }

            var allEmployees = await query.ToListAsync();
            SummaryTotalCount = allEmployees.Count;
            SummaryTotalPages = (int)Math.Ceiling(SummaryTotalCount / (double)SummaryPageSize);

            var pagedEmployees = allEmployees
                .Skip((SummaryCurrentPage - 1) * SummaryPageSize)
                .Take(SummaryPageSize)
                .ToList();

            // 2. Fetch attendance for these employees in range
            var attendance = await context.Attendances
                .Where(a => a.Date >= SummaryStartDate && a.Date <= SummaryEndDate)
                .ToListAsync();

            var today = DateTime.Today;
            var summaries = new List<AttendanceSummary>();

            foreach (var emp in pagedEmployees)
            {
                var summary = new AttendanceSummary
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeCode = emp.EmployeeCode,
                    Name = emp.FullName,
                    Department = emp.Department
                };

                var empAttendance = attendance.Where(a => a.EmployeeId == emp.EmployeeId).ToList();
                
                for (var date = SummaryStartDate; date <= SummaryEndDate; date = date.AddDays(1))
                {
                    var record = empAttendance.FirstOrDefault(a => a.Date.Date == date.Date);
                    if (record != null)
                    {
                        if (record.Status == "Đúng giờ" || record.Status == "Đi muộn") summary.WorkDays++;
                        if (record.Status == "Đi muộn") summary.LateTimes++;
                        if (record.Status == "Vắng") summary.AbsentDays++;
                        summary.OvertimeHours += record.OvertimeHours;
                    }
                    else if (date <= today && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        summary.AbsentDays++;
                    }
                }
                summaries.Add(summary);
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AttendanceSummaries = new ObservableCollection<AttendanceSummary>(summaries);
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải dữ liệu tổng hợp: " + ex.Message);
        }
        finally { IsBusy = false; }
    }

    private void ApplyFilters()
    {
        var filtered = _allEmployees.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(e => 
                e.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                e.EmployeeCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedDepartmentFilter != "Tất cả")
        {
            filtered = filtered.Where(e => e.Department == SelectedDepartmentFilter);
        }

        EmployeeProfiles = new ObservableCollection<Employee>(filtered);
    }

    public HRViewModel(
        IDbContextFactory<ManufacturingContext> contextFactory, 
        IAccessControlService accessControlService, 
        INotificationService notificationService,
        IHRService hrService,
        IUserManagementService userService,
        INavigationService navigationService,
        IAuthService authService,
        IAuditLogService auditLogService,
        IFileService fileService)
    {
        _contextFactory = contextFactory;
        _accessControlService = accessControlService;
        _notificationService = notificationService;
        _hrService = hrService;
        _userService = userService;
        _navigationService = navigationService;
        _authService = authService;
        _auditLogService = auditLogService;
        _fileService = fileService;
        
        SummaryMonthText = $"Tháng {SummaryMonth:MM, yyyy}";
        UpdateSummaryDateRange();
        UpdateReportColumns();
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        try 
        {
            await LoadPermissionsAsync();
            // 1. Load Employee Profiles
            var employees = await _hrService.GetEmployeesAsync();
            
            // Map legacy English statuses to Vietnamese for consistency
            foreach (var emp in employees)
            {
                if (emp.Status == "Active") emp.Status = "Đang làm việc";
                else if (emp.Status == "OnLeave") emp.Status = "Nghỉ phép";
                else if (emp.Status == "Inactive") emp.Status = "Nghỉ việc";
            }

            _allEmployees = employees;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AllEmployeesObservable = new ObservableCollection<Employee>(employees);
            });
            ApplyFilters(); 
            _ = LoadSummaryDataAsync();
            await LoadShiftsAsync();
            await LoadSchedulesAsync();
            
            TotalEmployees = employees.Count;
            ActiveEmployees = employees.Count(e => e.Status == "Đang làm việc");
            OnLeaveEmployees = employees.Count(e => e.Status == "Nghỉ phép");

            // 2. Load Payroll (Current Month)
            var now = DateTime.Now;
            var payroll = await _hrService.CalculateMonthlyPayrollAsync(now.Month, now.Year);
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PayrollRecords = new ObservableCollection<PayrollRecord>(payroll);
            });
            
            TotalSalaryText = (payroll.Sum(p => p.TotalSalary) / 1000000m).ToString("N1") + " triệu";
            TotalProductionQty = payroll.Sum(p => p.ProductionQty);
            ApprovedCount = payroll.Count(p => p.Status == "Đã duyệt");
            WaitingCount = payroll.Count(p => p.Status == "Chưa duyệt");

            // 3. Load Attendance (Today)
            var attendance = await _hrService.GetDailyAttendanceAsync(DateTime.Today);
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AttendanceRecords = new ObservableCollection<Attendance>(attendance);
            });
            
            // Load Today's schedules to determine timing status
            var todaySchedules = await _hrService.GetSchedulesAsync(DateTime.Today, DateTime.Today);
            
            // Build the unified list for the grid
            var dailyList = employees.Select(e => new EmployeeAttendanceItem
            {
                Employee = e,
                Record = attendance.FirstOrDefault(a => a.EmployeeId == e.EmployeeId),
                ScheduledShift = todaySchedules.FirstOrDefault(s => s.User?.EmployeeId == e.EmployeeId)?.Shift
            }).ToList();
            DailyAttendanceList = new ObservableCollection<EmployeeAttendanceItem>(dailyList);

            PresentCount = attendance.Count(a => a.Status == "Đúng giờ");
            LateCount = attendance.Count(a => a.Status == "Đi muộn");
            AbsentCount = attendance.Count(a => a.Status == "Vắng");

            // 4. Load Productivity Stats
            var stats = await _hrService.GetTopPerformersAsync(now.Month, now.Year);
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ProductivityChart = new ObservableCollection<ProductivityStats>(stats);
                TopPerformers = new ObservableCollection<ProductivityStats>(stats.Take(3));
            });

            // 5. Update Dates
            CurrentMonthText = $"tháng {now.Month}/{now.Year}";
            TodayText = DateTime.Today.ToString("dd/MM/yyyy");

            // 7. Update Not Checked In Stats
            NotCheckedInCount = DailyAttendanceList.Count(x => !x.IsCheckedIn);
            NotCheckedInMessage = NotCheckedInCount > 0 
                ? $"Phát hiện {NotCheckedInCount} nhân sự chưa điểm danh" 
                : "Tất cả nhân sự đã điểm danh";

            // 8. Initial Attendance Sheet Generation
            await GenerateAttendanceSheet();
            await GenerateScheduleMatrix();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải dữ liệu HR: " + ex.Message);
        }
    }

    [RelayCommand]
    private void PrevSchedulePage()
    {
        if (ScheduleCurrentPage > 1) ScheduleCurrentPage--;
    }

    [RelayCommand]
    private void NextSchedulePage()
    {
        if (ScheduleCurrentPage < ScheduleTotalPages) ScheduleCurrentPage++;
    }

    [RelayCommand]
    private async Task GenerateScheduleMatrix()
    {
        IsBusy = true;
        try 
        {
            var firstDay = new DateTime(ScheduleYear, ScheduleMonth, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            var daysInMonth = DateTime.DaysInMonth(ScheduleYear, ScheduleMonth);

            using var context = _contextFactory.CreateDbContext();
            
            // Filtering (use existing search text if any)
            var query = context.Employees.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(e => e.FullName.Contains(SearchText) || e.EmployeeCode.Contains(SearchText));
            }

            ScheduleTotalEmployees = await query.CountAsync();
            ScheduleTotalPages = (int)Math.Ceiling(ScheduleTotalEmployees / (double)SchedulePageSize);

            var employeesPage = await query
                .Include(e => e.UserAccount)
                .OrderBy(e => e.FullName)
                .Skip((ScheduleCurrentPage - 1) * SchedulePageSize)
                .Take(SchedulePageSize)
                .ToListAsync();

            var schedules = await context.EmployeeSchedules
                .Include(s => s.Shift)
                .Where(s => s.WorkDate >= DateOnly.FromDateTime(firstDay) && s.WorkDate <= DateOnly.FromDateTime(lastDay))
                .ToListAsync();

            var dt = new DataTable();
            dt.Columns.Add("Nhân viên", typeof(string));
            dt.Columns.Add("Mã NV", typeof(string));
            dt.Columns.Add("Ca làm việc", typeof(string));
            dt.Columns.Add("InternalUserId", typeof(int)); // Hidden link to schedule

            for (int i = 1; i <= daysInMonth; i++)
            {
                dt.Columns.Add(i.ToString(), typeof(string));
            }

            foreach (var emp in employeesPage)
            {
                var userId = emp.UserAccount?.UserId;
                var row = dt.NewRow();
                row["Nhân viên"] = emp.FullName;
                row["Mã NV"] = emp.EmployeeCode;
                row["InternalUserId"] = userId ?? 0;
                
                // Get the most common shift name for this employee in the month to show in the summary column
                var empSchedules = schedules.Where(s => s.UserId == userId).ToList();
                var mainShift = empSchedules.FirstOrDefault()?.Shift?.ShiftName ?? "-";
                row["Ca làm việc"] = mainShift;

                for (int i = 1; i <= daysInMonth; i++)
                {
                    var date = new DateOnly(ScheduleYear, ScheduleMonth, i);
                    var sch = schedules.FirstOrDefault(s => s.UserId == userId && s.WorkDate == date);
                    if (sch != null)
                    {
                        // Better display: "Sáng", "Chiều", "HC" instead of just "Ca"
                        string displayName = "Ca";
                        if (sch.Shift != null)
                        {
                            var name = sch.Shift.ShiftName;
                            if (name.Contains("sáng")) displayName = "Sáng";
                            else if (name.Contains("chiều")) displayName = "Chiều";
                            else if (name.Contains("đêm")) displayName = "Đêm";
                            else if (name.Contains("Hành chính")) displayName = "HC";
                            else displayName = name.Length > 5 ? name.Substring(0, 5) : name;
                        }
                        row[i.ToString()] = displayName;
                    }
                    else
                    {
                        row[i.ToString()] = "-";
                    }
                }
                dt.Rows.Add(row);
            }

            ScheduleMatrixData = dt;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tạo bảng lịch: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadPermissionsAsync()
    {
        await _accessControlService.RefreshAsync();
        var perms = ModulePermissionStateFactory.FromAccessControl(_accessControlService, SystemModules.HumanResources);
        CanAddHR = perms.CanAdd;
        CanEditHR = perms.CanEdit;
        CanDeleteHR = perms.CanDelete;
    }

    [RelayCommand]
    private void AddEmployee()
    {
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Nhân sự & Lương.");
            return;
        }

        _navigationService.NavigateTo<CreateEmployeeViewModel>();
    }

    [RelayCommand]
    private void EditEmployee(Employee employee)
    {
        if (employee == null) return;
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Nhân sự & Lương.");
            return;
        }

        var vm = _navigationService.NavigateTo<EditEmployeeViewModel>();
        vm.SetEmployee(employee);
    }

    [RelayCommand]
    private async Task DeleteEmployee(Employee employee)
    {
        if (employee == null) return;
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Delete))
        {
            _notificationService.ShowError("Bạn không có quyền Xóa trong phân hệ Nhân sự & Lương.");
            return;
        }

        bool confirmed = _notificationService.Confirm($"Bạn có chắc chắn muốn xóa nhân viên '{employee.FullName}'? Thao tác này sẽ xóa cả tài khoản người dùng liên quan và không thể hoàn tác.");
        if (!confirmed) return;

        try 
        {
            bool success = await _hrService.DeleteEmployeeAsync(employee.EmployeeId);
            if (success)
            {
                _notificationService.ShowSuccess($"Đã xóa nhân viên {employee.FullName} thành công.");
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowWarning($"Không thể xóa nhân viên '{employee.FullName}'. Có thể nhân sự này đã có dữ liệu lịch sử (Lệnh sản xuất, Kho bãi, Nhật ký...) gắn liền với tài khoản.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi xóa nhân viên: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveDraft()
    {
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Nhân sự & Lương.");
            return;
        }

        IsBusy = true;
        try
        {
            var now = DateTime.Now;
            bool success = await _hrService.SavePayrollAsync(PayrollRecords.ToList(), now.Month, now.Year);
            
            if (success)
            {
                _notificationService.ShowSuccess("Đã lưu bản nháp bảng lương vào cơ sở dữ liệu.");
            }
            else
            {
                _notificationService.ShowError("Lỗi khi lưu bản nháp bảng lương.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi hệ thống: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApprovePayroll()
    {
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Duyệt trong phân hệ Nhân sự & Lương.");
            return;
        }
        
        var now = DateTime.Now;
        foreach(var record in PayrollRecords)
        {
            record.Status = "Đã duyệt";
            record.ApprovedAt = now;
            record.ApprovedBy = _authService.CurrentUser?.UserId;
        }
        
        bool success = await _hrService.SavePayrollAsync(PayrollRecords.ToList(), now.Month, now.Year);
        
        if (success)
        {
            _notificationService.ShowSuccess("Đã duyệt và lưu toàn bộ bảng lương tháng này.");
            ApprovedCount = PayrollRecords.Count;
            WaitingCount = 0;
        }
        else
        {
            _notificationService.ShowError("Lỗi khi lưu bảng lương vào cơ sở dữ liệu.");
        }
    }

    [RelayCommand]
    private async Task ApproveSingle(PayrollRecord record)
    {
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Nhân sự & Lương.");
            return;
        }

        if (record != null) 
        {
            var originalStatus = record.Status;
            var now = DateTime.Now;
            record.Status = "Đã duyệt";
            record.ApprovedAt = now;
            record.ApprovedBy = _authService.CurrentUser?.UserId;
            
            bool success = await _hrService.SavePayrollAsync(PayrollRecords.ToList(), now.Month, now.Year);
            
            if (success)
            {
                ApprovedCount++;
                WaitingCount--;
                _notificationService.ShowSuccess($"Đã duyệt lương cho {record.Name}");
            }
            else
            {
                record.Status = originalStatus;
                _notificationService.ShowError("Lỗi khi cập nhật trạng thái phê duyệt.");
            }
        }
    }

    [RelayCommand]
    private async Task RecordAttendance(object parameter)
    {
        Employee? employee = parameter as Employee;
        if (employee == null && parameter is EmployeeAttendanceItem item) employee = item.Employee;
        
        if (employee == null) return;
        
        try 
        {
            // NEW: Get shift info for today
            var schedules = await _hrService.GetSchedulesAsync(DateTime.Today, DateTime.Today);
            var schedule = schedules.FirstOrDefault(s => s.User?.EmployeeId == employee.EmployeeId);
            
            TimeSpan startTime = new TimeSpan(8, 0, 0); // Default
            if (schedule?.Shift != null && schedule.Shift.StartTime.HasValue)
            {
                startTime = schedule.Shift.StartTime.Value.ToTimeSpan();
            }

            // If already has a record, check-out
            var existing = AttendanceRecords.FirstOrDefault(a => a.EmployeeId == employee.EmployeeId);
            string status = existing?.Status ?? "Đúng giờ";
            
            if (existing == null)
            {
                // Logic for late: > StartTime + 15 mins grace period
                if (DateTime.Now.TimeOfDay > startTime.Add(TimeSpan.FromMinutes(15))) status = "Đi muộn";
            }

            bool success = await _hrService.RecordAttendanceAsync(employee.EmployeeId, status);
            if (success)
            {
                _notificationService.ShowSuccess($"Đã ghi nhận chấm công cho {employee.FullName} (Ca: {schedule?.Shift?.ShiftName ?? "Hành chính"})");
                await LoadDataAsync(); 
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi chấm công: " + ex.Message);
        }
    }

    [RelayCommand]
    private void OpenAttendanceConsole()
    {
        _navigationService.NavigateTo<AttendanceConsoleViewModel>();
    }

    [RelayCommand]
    private async Task MarkAbsent(EmployeeAttendanceItem item)
    {
        if (item == null) return;
        
        try 
        {
            bool success = await _hrService.RecordAttendanceAsync(item.Employee.EmployeeId, "Vắng", "Nghỉ không phép");
            if (success)
            {
                _notificationService.ShowSuccess($"Đã đánh dấu vắng mặt cho {item.Employee.FullName}");
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi đánh dấu vắng: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task GenerateAttendanceSheet()
    {
        IsBusy = true;
        try 
        {
            // Calculate Date Range based on ViewMode
            if (ViewMode == "Tuần")
            {
                EndDate = DateTime.Today;
                StartDate = EndDate.AddDays(-6);
            }
            else if (ViewMode == "Tháng")
            {
                EndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
                StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }
            // If ViewMode is "Tùy chọn", we use the current StartDate/EndDate values set by user

            var daysCount = (EndDate - StartDate).Days + 1;
            if (daysCount > 62) daysCount = 62; // Safety limit for performance (2 months max)
            if (daysCount < 1) daysCount = 1;

            // Filtering
            var filteredEmployees = _allEmployees.AsEnumerable();
            if (SelectedDepartmentFilter != "Tất cả")
            {
                filteredEmployees = filteredEmployees.Where(e => e.Department == SelectedDepartmentFilter);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filteredEmployees = filteredEmployees.Where(e => 
                    e.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                    e.EmployeeCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Pagination
            var employeesList = filteredEmployees.ToList();
            TotalEmployeesCount = employeesList.Count;
            TotalPagesCount = (int)Math.Ceiling(TotalEmployeesCount / (double)PageSize);
            if (CurrentPage > TotalPagesCount && TotalPagesCount > 0) CurrentPage = TotalPagesCount;

            var employeesPage = employeesList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Fetch Attendance for the whole range
            using var context = _contextFactory.CreateDbContext();
            var attendanceInRange = await context.Attendances
                .Where(a => a.Date >= StartDate && a.Date <= EndDate)
                .ToListAsync();

            // Create DataTable
            var dt = new DataTable();
            dt.Columns.Add("IsSelected", typeof(bool));
            dt.Columns.Add("STT", typeof(int));
            dt.Columns.Add("EmployeeInfo", typeof(string)); // FullName (Code)
            dt.Columns.Add("Phòng ban", typeof(string));
            dt.Columns.Add("EmployeeId", typeof(int));

            for (int i = 0; i < daysCount; i++)
            {
                var date = StartDate.AddDays(i);
                dt.Columns.Add(date.ToString("dd/MM"), typeof(string));
            }

            // Fill Rows
            var summaries = new List<AttendanceSummary>();
            int stt = (CurrentPage - 1) * PageSize + 1;
            
            // First, calculate summaries for ALL filtered employees (for the summary table)
            foreach (var emp in employeesList)
            {
                var summary = new AttendanceSummary 
                { 
                    Name = emp.FullName,
                    EmployeeCode = emp.EmployeeCode,
                    Department = emp.Department,
                    EmployeeId = emp.EmployeeId
                };

                for (int i = 0; i < daysCount; i++)
                {
                    var date = StartDate.AddDays(i);
                    var record = attendanceInRange.FirstOrDefault(a => a.EmployeeId == emp.EmployeeId && a.Date.Date == date.Date);
                    
                    if (record != null) 
                    {
                        if (record.Status == "Vắng") summary.AbsentDays++;
                        else if (record.Status == "Đi muộn") 
                        {
                            summary.LateTimes++;
                            summary.WorkDays++;
                        }
                        else summary.WorkDays++;

                        summary.OvertimeHours += record.OvertimeHours;
                    }
                }
                summaries.Add(summary);
            }

            // Then, fill the DataTable for the Matrix View (only for current page)
            foreach (var emp in employeesPage)
            {
                var row = dt.NewRow();
                row["IsSelected"] = false;
                row["STT"] = stt++;
                row["EmployeeInfo"] = $"{emp.FullName}\n({emp.EmployeeCode})";
                row["Phòng ban"] = emp.Department ?? "-";
                row["EmployeeId"] = emp.EmployeeId;

                for (int i = 0; i < daysCount; i++)
                {
                    var date = StartDate.AddDays(i);
                    var record = attendanceInRange.FirstOrDefault(a => a.EmployeeId == emp.EmployeeId && a.Date.Date == date.Date);
                    
                    if (record == null) row[date.ToString("dd/MM")] = "—";
                    else if (record.Status == "Vắng") row[date.ToString("dd/MM")] = "V";
                    else if (record.Status == "Đi muộn") row[date.ToString("dd/MM")] = "M";
                    else row[date.ToString("dd/MM")] = "X";
                }
                dt.Rows.Add(row);
            }

            AttendanceSheetData = dt;
            
            // Only update KPIs here, AttendanceSummaries is handled by LoadSummaryDataAsync
            PresentCount = summaries.Sum(s => s.WorkDays);
            AbsentCount = summaries.Sum(s => s.AbsentDays);
            LateCount = summaries.Sum(s => s.LateTimes);
            TotalOvertimeHours = summaries.Sum(s => s.OvertimeHours);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tạo bảng chấm công: " + ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPagesCount)
        {
            CurrentPage++;
            await GenerateAttendanceSheet();
        }
    }

    [RelayCommand]
    private async Task PrevPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await GenerateAttendanceSheet();
        }
    }

    [RelayCommand]
    private async Task SetViewMode(string mode)
    {
        ViewMode = mode;
        CurrentPage = 1;
        await GenerateAttendanceSheet();
    }
    [RelayCommand]
    public async Task LoadShiftsAsync()
    {
        var list = await _hrService.GetShiftsAsync();
        Shifts = new ObservableCollection<Shift>(list);
        if (Shifts.Any() && SelectedShift == null) SelectedShift = Shifts[0];
    }

    [RelayCommand]
    private async Task AddShift()
    {
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Nhân sự & Lương.");
            return;
        }

        var dialog = new Views.Dialogs.ShiftDialog();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            await SaveShift(dialog.Shift);
        }
    }

    [RelayCommand]
    private async Task EditShift(Shift shift)
    {
        if (shift == null) return;
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Nhân sự & Lương.");
            return;
        }
        
        var dialog = new Views.Dialogs.ShiftDialog(shift);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            await SaveShift(dialog.Shift);
        }
    }

    [RelayCommand]
    private async Task SaveShift(Shift shift)
    {
        if (shift == null) return;

        // 1. Ràng buộc dữ liệu cơ bản
        if (string.IsNullOrWhiteSpace(shift.ShiftName))
        {
            _notificationService.ShowError("Tên ca làm việc không được để trống.");
            return;
        }

        // 2. Kiểm tra trùng mã/tên (chỉ áp dụng khi thêm mới hoặc chỉnh sửa)
        var duplicateName = Shifts.FirstOrDefault(s => s.ShiftName.Equals(shift.ShiftName, StringComparison.OrdinalIgnoreCase) && s.ShiftId != shift.ShiftId);
        if (duplicateName != null)
        {
            _notificationService.ShowError($"Tên ca '{shift.ShiftName}' đã tồn tại.");
            return;
        }

        if (shift.StartTime >= shift.EndTime)
        {
            _notificationService.ShowError("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.");
            return;
        }

        if (shift.BreakStartTime < shift.StartTime || shift.BreakEndTime > shift.EndTime)
        {
            _notificationService.ShowError("Giờ nghỉ giải lao phải nằm trong khung giờ làm việc.");
            return;
        }

        bool isNew = shift.ShiftId == 0;
        bool success;
        
        // Chuẩn bị thông tin Audit Log chi tiết
        string action = isNew ? "Thêm" : "Sửa";
        string oldValue = "-";
        string newValue = $"Tên: {shift.ShiftName} | Giờ: {shift.StartTime}-{shift.EndTime}";

        if (!isNew)
        {
            // Lấy dữ liệu cũ từ database để so sánh (không dùng proxy trong collection)
            using var context = _contextFactory.CreateDbContext();
            var original = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.ShiftId == shift.ShiftId);
            if (original != null)
            {
                oldValue = $"Tên: {original.ShiftName} | Giờ: {original.StartTime}-{original.EndTime} | Nghỉ: {original.BreakStartTime}-{original.BreakEndTime}";
            }
        }

        if (isNew) success = await _hrService.AddShiftAsync(shift) != null;
        else success = await _hrService.UpdateShiftAsync(shift);

        if (success)
        {
            _notificationService.ShowSuccess($"Đã {(isNew ? "thêm mới" : "cập nhật")} ca '{shift.ShiftName}' thành công.");
            
            // Ghi Audit Log chi tiết
            var user = _authService.CurrentUser;
            await _auditLogService.LogAsync(user?.UserId, action, "Shifts", oldValue, newValue);

            await LoadShiftsAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteShift(Shift shift)
    {
        if (shift == null) return;
        if (!_accessControlService.HasCached(SystemModules.HumanResources, PermissionAction.Delete))
        {
            _notificationService.ShowError("Bạn không có quyền Xóa trong phân hệ Nhân sự & Lương.");
            return;
        }

        bool confirm = _notificationService.Confirm(
            $"Bạn có chắc chắn muốn xóa ca làm việc '{shift.ShiftName}' không? Hành động này không thể hoàn tác.", 
            "Xác nhận xóa");

        if (!confirm) return;

        try
        {
            var success = await _hrService.DeleteShiftAsync(shift.ShiftId);
            if (success)
            {
                _notificationService.ShowSuccess($"Đã xóa ca làm việc '{shift.ShiftName}' thành công.");
                
                // Lưu Audit Log
                var user = _authService.CurrentUser;
                await _auditLogService.LogAsync(
                    user?.UserId, 
                    "Xóa", 
                    "Shifts", 
                    $"Ca: {shift.ShiftName} | ID: {shift.ShiftId}", 
                    "-"
                );

                await LoadShiftsAsync();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Không thể xóa ca làm việc: " + ex.Message);
        }
    }

    [RelayCommand]
    public async Task LoadSchedulesAsync()
    {
        // Load for current month
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var list = await _hrService.GetSchedulesAsync(start, end);
        EmployeeSchedules = new ObservableCollection<EmployeeSchedule>(list);
        await GenerateScheduleMatrix();
    }

    [RelayCommand]
    private async Task ViewShiftDetails(Shift shift)
    {
        if (shift == null) return;
        
        // Fetch employees assigned to this shift for today
        var schedules = await _hrService.GetSchedulesAsync(DateTime.Today, DateTime.Today);
        var assignedEmployees = schedules
            .Where(s => s.ShiftId == shift.ShiftId && s.User?.Employee != null)
            .Select(s => s.User!.Employee!)
            .ToList();

        var dialog = new Views.Dialogs.ShiftDetailDialog(shift, assignedEmployees);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.ShowDialog();
    }

    [RelayCommand]
    private async Task AssignSchedule()
    {
        // 1. Get list of users and shifts
        using var context = _contextFactory.CreateDbContext();
        var users = await context.Users.Include(u => u.Employee).OrderBy(u => u.Username).ToListAsync();
        var shifts = await _hrService.GetShiftsAsync();

        var dialog = new Views.Dialogs.ScheduleDialog(users, shifts);
        dialog.Owner = System.Windows.Application.Current.MainWindow;

        if (dialog.ShowDialog() == true && dialog.Results.Any())
        {
            int successCount = 0;
            foreach (var schedule in dialog.Results)
            {
                var success = await _hrService.AssignScheduleAsync(schedule);
                if (success) successCount++;
            }

            if (successCount > 0)
            {
                _notificationService.ShowSuccess($"Đã gán thành công {successCount} lượt trực.");
                
                // Audit Log
                var user = _authService.CurrentUser;
                await _auditLogService.LogAsync(
                    user?.UserId, "Phân lịch hàng loạt", "EmployeeSchedules", "-", 
                    $"Đã gán {successCount} lượt trực cho nhóm nhân viên từ {dialog.Results.Min(s => s.WorkDate)} đến {dialog.Results.Max(s => s.WorkDate)}"
                );

                await LoadSchedulesAsync();
                await GenerateScheduleMatrix();
            }
        }
    }

    [RelayCommand]
    private void OpenScheduleImportExport()
    {
        _navigationService.NavigateTo<ScheduleImportExportViewModel>();
    }

    [RelayCommand]
    private async Task EditScheduleCell(object parameter)
    {
        if (parameter is not object[] values || values.Length < 2) return;
        var rowView = values[0] as DataRowView;
        var columnName = values[1] as string;

        if (rowView == null || string.IsNullOrEmpty(columnName) || columnName == "Nhân viên" || columnName == "Mã NV" || columnName == "Ca làm việc") return;

        int userId = (int)rowView.Row["InternalUserId"];
        if (userId == 0)
        {
            _notificationService.ShowWarning("Nhân viên này chưa có tài khoản hệ thống để phân lịch.");
            return;
        }

        if (!int.TryParse(columnName, out int day)) return;
        
        var date = new DateOnly(ScheduleYear, ScheduleMonth, day);

        using var context = _contextFactory.CreateDbContext();
        var schedule = await context.EmployeeSchedules
            .Include(s => s.User)
            .Include(s => s.Shift)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.WorkDate == date);

        if (schedule == null)
        {
            _notificationService.ShowInfo($"Ngày {date:dd/MM} chưa có lịch trực. Vui lòng dùng nút 'Phân lịch'.");
            return;
        }

        // Open Dialog
        var users = await context.Users.Include(u => u.Employee).ToListAsync();
        var shifts = await _hrService.GetShiftsAsync();
        var dialog = new Views.Dialogs.ScheduleDialog(users, shifts, null, schedule);
        dialog.Owner = System.Windows.Application.Current.MainWindow;

        if (dialog.ShowDialog() == true && dialog.Results.Any())
        {
            var results = dialog.Results;
            int successCount = 0;
            
            IsBusy = true;
            try
            {
                foreach (var res in results)
                {
                    // AssignScheduleAsync handles both adding new and updating existing
                    var success = await _hrService.AssignScheduleAsync(res);
                    if (success) successCount++;
                }

                if (successCount > 0)
                {
                    _notificationService.ShowSuccess($"Đã cập nhật thành công {successCount} lượt trực.");
                    await LoadSchedulesAsync();
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi cập nhật lịch: " + ex.Message);
            }
            finally { IsBusy = false; }
        }
    }

    [RelayCommand]
    private async Task DeleteScheduleCell(object parameter)
    {
        if (parameter is not object[] values || values.Length < 2) return;
        var rowView = values[0] as DataRowView;
        var columnName = values[1] as string;

        if (rowView == null || string.IsNullOrEmpty(columnName) || columnName == "Nhân viên" || columnName == "Mã NV" || columnName == "Ca làm việc") return;

        int userId = (int)rowView.Row["InternalUserId"];
        if (userId == 0) return;

        if (!int.TryParse(columnName, out int day)) return;
        
        var date = new DateOnly(ScheduleYear, ScheduleMonth, day);

        using var context = _contextFactory.CreateDbContext();
        var schedule = await context.EmployeeSchedules
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.WorkDate == date);

        if (schedule == null)
        {
            _notificationService.ShowInfo($"Ngày {date:dd/MM} đang trống, không có lịch để xóa.");
            return;
        }

        // Open Dialog in Delete Mode
        var users = await context.Users.Include(u => u.Employee).ToListAsync();
        var shifts = await _hrService.GetShiftsAsync();
        
        var dialog = new Views.Dialogs.ScheduleDialog(users, shifts, null, schedule, isDeleteMode: true);
        dialog.Owner = System.Windows.Application.Current.MainWindow;

        if (dialog.ShowDialog() == true && dialog.Results.Any())
        {
            var results = dialog.Results;
            int totalDeleted = 0;
            
            IsBusy = true;
            try
            {
                foreach (var res in results)
                {
                    var toDelete = await context.EmployeeSchedules
                        .FirstOrDefaultAsync(s => s.UserId == res.UserId && s.WorkDate == res.WorkDate);
                    
                    if (toDelete != null)
                    {
                        context.EmployeeSchedules.Remove(toDelete);
                        totalDeleted++;
                    }
                }

                if (totalDeleted > 0)
                {
                    await context.SaveChangesAsync();
                    _notificationService.ShowSuccess($"Đã hủy thành công {totalDeleted} lượt trực.");
                    await LoadSchedulesAsync();
                }
                else
                {
                    _notificationService.ShowWarning("Không tìm thấy lượt trực nào trong khoảng thời gian đã chọn để xóa.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi hủy lịch: " + ex.Message);
            }
            finally { IsBusy = false; }
        }
    }

    [RelayCommand]
    private async Task DeleteSchedule(EmployeeSchedule schedule)
    {
        if (schedule == null) return;

        bool confirm = _notificationService.Confirm($"Xóa lịch trực ngày {schedule.WorkDate} của nhân viên {schedule.User?.FullName}?", "Xác nhận xóa");
        if (!confirm) return;

        var success = await _hrService.RemoveScheduleAsync(schedule.ScheduleId);
        if (success)
        {
            _notificationService.ShowSuccess("Đã xóa lịch trực.");
            await LoadSchedulesAsync();
            await GenerateScheduleMatrix();
        }
    }
}

public class FlatPayrollExport
{
    public string MonthYear { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public decimal ProductionSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal Deduction { get; set; }
    public decimal TotalSalary { get; set; }
}
