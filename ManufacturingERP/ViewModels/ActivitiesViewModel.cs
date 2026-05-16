using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.Core;
using Microsoft.Win32;

namespace ManufacturingERP.ViewModels;

public partial class ActivitiesViewModel : ViewModelBase
{
    private readonly IActivityService _activityService;
    private readonly INavigationService _navigationService;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private ObservableCollection<RecentActivity> _allActivities = new();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedType = "Tất cả";
    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;

    public List<string> ActivityTypes { get; } = new() { "Tất cả", "Thêm", "Sửa", "Xóa", "Hệ thống" };

    public ActivitiesViewModel(
        IActivityService activityService, 
        INavigationService navigationService,
        IFileService fileService,
        INotificationService notificationService)
    {
        _activityService = activityService;
        _navigationService = navigationService;
        _fileService = fileService;
        _notificationService = notificationService;
    }

    // Change from override to a normal method to avoid potential metadata issues with ViewModelBase
    public async Task LoadInitialDataAsync()
    {
        await LoadAllActivitiesAsync();
    }

    [RelayCommand]
    private async Task LoadAllActivitiesAsync()
    {
        try 
        {
            var logs = await _activityService.GetRecentActivitiesAsync(500);
            var filtered = logs.AsEnumerable();

            if (!string.IsNullOrEmpty(SearchText))
                filtered = filtered.Where(l => l.Content?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true || 
                                              l.PerformedBy?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);

            if (SelectedType != "Tất cả")
                filtered = filtered.Where(l => l.ActivityType == SelectedType || (SelectedType == "Sửa" && l.ActivityType == "Cập nhật"));

            filtered = filtered.Where(l => l.Timestamp?.Date >= StartDate.Date && l.Timestamp?.Date <= EndDate.Date);

            AllActivities.Clear();
            foreach (var log in filtered.OrderByDescending(l => l.Timestamp))
            {
                AllActivities.Add(new RecentActivity
                {
                    Type = log.ActivityType == "Cập nhật" ? "Sửa" : log.ActivityType,
                    Content = log.Content,
                    User = log.PerformedBy,
                    Time = log.Timestamp?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"
                });
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi khi tải nhật ký hoạt động: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Back()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
    }

    [RelayCommand]
    private async Task ExportReport()
    {
        if (AllActivities == null || !AllActivities.Any())
        {
            _notificationService.ShowError("Không có dữ liệu nhật ký để xuất báo cáo.");
            return;
        }

        var fileName = $"Nhat_ky_Hoat_dong_{DateTime.Now:yyyyMMdd_HHmm}";
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = fileName
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            var columns = new List<string> { "Hành động", "Nội dung", "Người thực hiện", "Thời gian" };
            
            // Map data to a simple list for export
            var exportData = AllActivities.Select(a => new
            {
                Type = a.Type,
                Content = a.Content,
                User = a.User,
                Time = a.Time
            }).ToList();

            var success = await _fileService.ExportToExcelAsync(exportData, saveFileDialog.FileName, "Nhật ký", columns);
            
            if (success)
            {
                _notificationService.ShowSuccess("Xuất báo cáo nhật ký thành công!");
            }
            else
            {
                _notificationService.ShowError("Lỗi: Không thể lưu file báo cáo. Hãy thử lại.");
            }
        }
    }

    [RelayCommand]
    private void OpenDetail(RecentActivity activity)
    {
        if (activity == null) return;
        var vm = _navigationService.NavigateTo<ActivityDetailViewModel>();
        vm.Initialize(activity);
    }

    partial void OnSearchTextChanged(string value) => _ = LoadAllActivitiesAsync();
    partial void OnSelectedTypeChanged(string value) => _ = LoadAllActivitiesAsync();
    partial void OnStartDateChanged(DateTime value) => _ = LoadAllActivitiesAsync();
    partial void OnEndDateChanged(DateTime value) => _ = LoadAllActivitiesAsync();
}
