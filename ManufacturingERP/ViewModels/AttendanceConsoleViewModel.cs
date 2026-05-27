using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Core;

namespace ManufacturingERP.ViewModels;

public partial class AttendanceConsoleViewModel : ViewModelBase
{
    private readonly IHRService _hrService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;

    [ObservableProperty] private ObservableCollection<EmployeeAttendanceItem> _employees = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedDepartment = "Tất cả";
    [ObservableProperty] private int _presentCount;
    [ObservableProperty] private int _lateCount;
    [ObservableProperty] private int _absentCount;
    [ObservableProperty] private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _isAnySelected;

    private List<EmployeeAttendanceItem> _allData = new();

    public ObservableCollection<string> Departments { get; } = new() { "Tất cả", "Sản xuất", "Kho bãi", "Kỹ thuật", "Hành chính", "Kế toán" };

    public AttendanceConsoleViewModel(
        IHRService hrService, 
        INavigationService navigationService, 
        INotificationService notificationService,
        IDbContextFactory<ManufacturingContext> contextFactory)
    {
        _hrService = hrService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _contextFactory = contextFactory;
        
        _ = LoadDataAsync();
        
        // Timer for real-time clock
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        timer.Start();
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try 
        {
            var allEmployees = await _hrService.GetEmployeesAsync();
            var attendance = await _hrService.GetDailyAttendanceAsync(DateTime.Today);
            var todaySchedules = await _hrService.GetSchedulesAsync(DateTime.Today, DateTime.Today);
            
            _allData = allEmployees.Select(e => {
                var item = new EmployeeAttendanceItem
                {
                    Employee = e,
                    Record = attendance.FirstOrDefault(a => a.EmployeeId == e.EmployeeId),
                    ScheduledShift = todaySchedules.FirstOrDefault(s => s.User?.EmployeeId == e.EmployeeId)?.Shift
                };
                item.PropertyChanged += (s, ev) => {
                    if (ev.PropertyName == nameof(EmployeeAttendanceItem.IsSelected))
                        UpdateSelectionState();
                };
                return item;
            }).ToList();
            
            ApplyFilters();
            UpdateStats();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi tải dữ liệu chấm công: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateStats()
    {
        PresentCount = _allData.Count(e => e.TimingStatus == "ĐÚNG GIỜ");
        LateCount = _allData.Count(e => e.TimingStatus == "ĐI MUỘN");
        AbsentCount = _allData.Count(e => e.TimingStatus == "VẮNG MẶT");
    }

    partial void OnSearchTextChanged(string value) { CurrentPage = 1; ApplyFilters(); }
    partial void OnSelectedDepartmentChanged(string value) { CurrentPage = 1; ApplyFilters(); }
    partial void OnCurrentPageChanged(int value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allData.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(e => 
                e.Employee.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                e.Employee.EmployeeCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedDepartment != "Tất cả")
        {
            filtered = filtered.Where(e => e.Employee.Department == SelectedDepartment);
        }

        var filteredList = filtered.OrderBy(e => e.Record != null).ToList();
        TotalCount = filteredList.Count;
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

        var paged = filteredList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Employees = new ObservableCollection<EmployeeAttendanceItem>(paged);
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages) CurrentPage++;
    }

    [RelayCommand]
    private void PrevPage()
    {
        if (CurrentPage > 1) CurrentPage--;
    }

    [RelayCommand]
    private async Task BatchMarkAttendance(string status)
    {
        var selected = Employees.Where(e => e.IsSelected && e.Record == null).ToList();
        if (!selected.Any())
        {
            _notificationService.ShowWarning("Vui lòng chọn nhân viên chưa điểm danh");
            return;
        }

        int count = 0;
        foreach (var item in selected)
        {
            string finalStatus = status;
            if (status == "Auto")
            {
                finalStatus = "Đúng giờ";
                if (item.ScheduledShift != null && item.ScheduledShift.StartTime != null)
                {
                    var now = TimeOnly.FromDateTime(DateTime.Now);
                    if (now > item.ScheduledShift.StartTime.Value.AddMinutes(15))
                    {
                        finalStatus = "Đi muộn";
                    }
                }
            }

            bool success = await _hrService.RecordAttendanceAsync(item.Employee.EmployeeId, finalStatus);
            if (success) count++;
        }

        if (count > 0)
        {
            _notificationService.ShowSuccess($"Đã ghi nhận thành công cho {count} nhân viên");
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private void SelectAll(bool isSelected)
    {
        foreach (var item in Employees)
        {
            item.IsSelected = isSelected;
        }
        UpdateSelectionState();
    }

    [RelayCommand]
    private void UpdateSelection()
    {
        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        IsAnySelected = Employees.Any(e => e.IsSelected);
    }

    [RelayCommand]
    private async Task MarkAttendance(EmployeeAttendanceItem item)
    {
        if (item == null || item.Record != null) return;
        
        string status = "Đúng giờ";
        if (item.ScheduledShift != null && item.ScheduledShift.StartTime != null)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            if (now > item.ScheduledShift.StartTime.Value.AddMinutes(15))
            {
                status = "Đi muộn";
            }
        }

        bool success = await _hrService.RecordAttendanceAsync(item.Employee.EmployeeId, status);
        if (success)
        {
            _notificationService.ShowSuccess($"Đã ghi nhận có mặt cho {item.Employee.FullName} ({status})");
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task MarkLate(EmployeeAttendanceItem item)
    {
        if (item == null || item.Record != null) return;
        
        bool success = await _hrService.RecordAttendanceAsync(item.Employee.EmployeeId, "Đi muộn");
        if (success)
        {
            _notificationService.ShowSuccess($"Đã ghi nhận đi muộn cho {item.Employee.FullName}");
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task MarkAbsent(EmployeeAttendanceItem item)
    {
        if (item == null || item.Record != null) return;
        
        bool success = await _hrService.RecordAttendanceAsync(item.Employee.EmployeeId, "Vắng", "Nghỉ không phép");
        if (success)
        {
            _notificationService.ShowSuccess($"Đã đánh dấu vắng cho {item.Employee.FullName}");
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task MarkCheckOut(EmployeeAttendanceItem item)
    {
        if (item == null || item.Record == null || item.IsCheckedOut) return;
        
        var success = await _hrService.RecordAttendanceAsync(item.Employee.EmployeeId, item.Record.Status);
        if (success)
        {
            _notificationService.ShowSuccess($"Đã ghi nhận ra ca cho {item.Employee.FullName}");
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task ResetAttendance(EmployeeAttendanceItem item)
    {
        if (item == null || item.Record == null) return;
        
        bool confirm = _notificationService.Confirm(
            $"Bạn có chắc chắn muốn HỦY trạng thái chấm công của {item.Employee.FullName} ngày hôm nay?",
            "Xác nhận sửa sai");
        
        if (!confirm) return;

        using var context = _contextFactory.CreateDbContext();
        var record = await context.Attendances.FindAsync(item.Record.AttendanceId);
        if (record != null)
        {
            context.Attendances.Remove(record);
            await context.SaveChangesAsync();
            _notificationService.ShowSuccess($"Đã xóa trạng thái của {item.Employee.FullName}. Bạn có thể điểm danh lại.");
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateTo<HRViewModel>();
    }
}
