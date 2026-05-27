using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;
using ManufacturingERP.Core;
using ManufacturingERP.Services;
using System.Threading.Tasks;
using System.Linq;
using ManufacturingERP.Views.Dialogs;
using ManufacturingERP.Models;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Threading;

namespace ManufacturingERP.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IProductionService _productionService;
    private readonly INavigationService _navigationService;
    private readonly IActivityService _activityService;
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IQualityControlService _qualityControlService;





    // Stats
    [ObservableProperty] private string _runningOrders = "0";
    [ObservableProperty] private string _materialAlerts = "8";
    [ObservableProperty] private string _todayProductivity = "94.2%";
    [ObservableProperty] private string _monthlyRevenue = "1.2 tỷ";

    // Charts
    [ObservableProperty] private SeriesCollection? _productionSeries;
    [ObservableProperty] private SeriesCollection? _defectSeries;
    [ObservableProperty] private string[]? _productionLabels;

    // Tables
    public ObservableCollection<UrgentOrder> UrgentOrders { get; } = new();
    public ObservableCollection<RecentActivity> Activities { get; } = new();
    public ObservableCollection<DefectStatItem> DefectReasonStats { get; } = new();

    [ObservableProperty]
    private string _selectedDefectTitle = "Chưa có dữ liệu";

    [ObservableProperty]
    private double _selectedDefectValue = 0;

    [ObservableProperty]
    private string _qcSummaryText = "Chưa có dữ liệu kiểm tra QC";

    private int _qcTotalSamples = 0;
    private readonly DispatcherTimer _autoRefreshTimer;

    public DashboardViewModel(
        IProductionService productionService, 
        INavigationService navigationService, 
        IActivityService activityService, 
        INotificationService notificationService, 
        IFileService fileService,
        IQualityControlService qualityControlService)
    {
        _productionService = productionService;
        _navigationService = navigationService;
        _activityService = activityService;
        _notificationService = notificationService;
        _fileService = fileService;
        _qualityControlService = qualityControlService;

        
        LoadMockData();

        _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autoRefreshTimer.Tick += async (s, e) => await LoadDashboardDataAsync();

        _ = LoadDashboardDataAsync();
    }

    public async Task InitializeAsync()
    {
        await LoadDashboardDataAsync();
        if (!_autoRefreshTimer.IsEnabled) _autoRefreshTimer.Start();
    }

    public async Task LogActivityAsync(string type, string content)
    {
        await _activityService.LogActivityAsync(type, content);
        await LoadDashboardDataAsync(); // Refresh to show the new activity
    }

    public async Task LoadDashboardDataAsync()
    {
        try
        {
        var orders = await _productionService.GetRecentWorkOrdersAsync(10);
        
        // Update Stats based on real data
        var runningOrdersCount = orders.Count(o => o.Status == "Running" || o.Status == "Đang sản xuất");
        RunningOrders = runningOrdersCount.ToString();

        var stats = await _productionService.GetDashboardStatsAsync();
        MaterialAlerts = stats.MaterialAlerts.ToString();
        TodayProductivity = $"{stats.TodayProductivity}%";
        
        // Format revenue (e.g., 1.2 tỷ or VNĐ)
        if (stats.MonthlyRevenue >= 1000000000)
            MonthlyRevenue = $"{(stats.MonthlyRevenue / 1000000000):N1} tỷ";
        else if (stats.MonthlyRevenue >= 1000000)
            MonthlyRevenue = $"{(stats.MonthlyRevenue / 1000000):N1} tr";
        else
            MonthlyRevenue = $"{stats.MonthlyRevenue:N0} đ";

        // Load Chart Data (Production)
        var chartData = await _productionService.GetProductionChartDataAsync(30);
        ProductionLabels = chartData.Labels.ToArray();

        var lineColor = System.Windows.Media.Color.FromRgb(37, 99, 235);
        var planLineColor = System.Windows.Media.Color.FromRgb(239, 68, 68);

        ProductionSeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Sản lượng đạt",
                Values = new ChartValues<int>(chartData.ProductionValues),
                Stroke = new System.Windows.Media.SolidColorBrush(lineColor),
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 37, 99, 235)),
                PointGeometrySize = 8,
                StrokeThickness = 2.5
            },
            new LineSeries
            {
                Title = "Kế hoạch",
                Values = new ChartValues<int>(chartData.PlanValues),
                Stroke = new System.Windows.Media.SolidColorBrush(planLineColor),
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 6, 3 },
                StrokeThickness = 2,
                PointGeometrySize = 0
            }
        };

        // Load QC Statistics
        int qcPassed = 0, qcFailed = 0;
        var qcDefectStats = new List<(string Category, int Count)>();
        try
        {
            var startDate = DateTime.Now.AddMonths(-1);
            var endDate = DateTime.Now;
            var qcStats = await _qualityControlService.GetStatisticsAsync(startDate, endDate);
            qcPassed = qcStats.Passed;
            qcFailed = qcStats.Failed;
            qcDefectStats = qcStats.DefectStats;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard QC stats error: {ex.Message}");
        }

        // QC Pie Chart
        _qcTotalSamples = qcPassed + qcFailed;
        if (_qcTotalSamples > 0)
        {
            DefectSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Đạt QC",
                    Values = new ChartValues<int> { qcPassed },
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "Lỗi QC",
                    Values = new ChartValues<int> { qcFailed },
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                    DataLabels = true
                }
            };
            SelectedDefectTitle = "Tổng mẫu";
            SelectedDefectValue = _qcTotalSamples;
            QcSummaryText = $"Tổng: {qcPassed:N0} đạt / {qcFailed:N0} lỗi";
        }
        else
        {
            DefectSeries = new SeriesCollection();
            SelectedDefectTitle = "Chưa có dữ liệu";
            SelectedDefectValue = 0;
            QcSummaryText = "Chưa có dữ liệu kiểm tra QC";
        }

        // Load Defect Reason Statistics
        DefectReasonStats.Clear();
        var realDefects = qcDefectStats
            .Where(d => d.Category != "Đạt chuẩn")
            .ToList();
        int totalRealDefects = realDefects.Sum(d => d.Count);
        foreach (var stat in realDefects)
        {
            double percentage = totalRealDefects > 0 ? (double)stat.Count / totalRealDefects * 100 : 0;
            DefectReasonStats.Add(new DefectStatItem
            {
                Reason = stat.Category,
                Count = stat.Count,
                Percentage = percentage
            });
        }

        // Update Table
        UrgentOrders.Clear();
        foreach (var wo in orders.OrderByDescending(o => o.Woid))
        {
            var firstItem = wo.WorkOrderItems.FirstOrDefault();
            var itemsCount = wo.WorkOrderItems.Count;
            
            var productName = firstItem?.Product?.MaterialName ?? "N/A";
            if (itemsCount > 1) 
                productName += $" (+{itemsCount - 1} khác)";

            var totalTarget = wo.WorkOrderItems.Sum(i => i.TargetQty);
            var totalActual = wo.WorkOrderItems.Sum(i => i.ActualQty);
            var overallProgress = totalTarget > 0 ? (int)(totalActual * 100 / totalTarget) : 0;

            var translatedStatus = wo.Status switch
            {
                "Planned" => "Chờ",
                "Running" => "Đang sản xuất",
                "Completed" => "Hoàn thành",
                "Paused" => "Tạm dừng",
                "Cancelled" => "Hủy",
                _ => wo.Status ?? "Chờ"
            };

            UrgentOrders.Add(new UrgentOrder
            {
                Code = wo.Wocode,
                Product = productName,
                Quantity = totalTarget,
                Status = translatedStatus,
                IsUrgent = wo.IsUrgent,
                Deadline = wo.EndDate?.ToString("dd/MM/yyyy") ?? "N/A",
                Progress = overallProgress
            });
        }

        // Update Activities from DB
        var dbActivities = await _activityService.GetRecentActivitiesAsync(10);
        Activities.Clear();
        foreach (var act in dbActivities)
        {
            Activities.Add(new RecentActivity
            {
                Type = act.ActivityType == "Cập nhật" ? "Sửa" : act.ActivityType,
                Content = act.Content,
                User = act.PerformedBy ?? "N/A",
                Time = act.Timestamp?.ToString("HH:mm") ?? "N/A"
            });
        }
        }
        catch (System.Exception ex)
        {
            _notificationService.ShowError($"Lỗi tải dữ liệu Dashboard: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshDashboard()
    {
        if (!_autoRefreshTimer.IsEnabled) _autoRefreshTimer.Start();
        await LoadDashboardDataAsync();
        _notificationService.ShowSuccess("Đã làm mới dữ liệu");
    }

    [RelayCommand]
    private void ViewMaterialAlerts()
    {
        _navigationService.NavigateTo<WarehouseViewModel>();
    }

    [RelayCommand]
    private async Task AddWorkOrder()
    {
        _navigationService.NavigateTo<CreateWorkOrderViewModel>();
        if (_navigationService.CurrentView is CreateWorkOrderViewModel createVm)
        {
            await createVm.InitializeAsync();
        }
    }

    [RelayCommand]
    private void NavigateImportExport()
    {
        _navigationService.NavigateTo<ImportExportViewModel>();
    }

    private void LoadMockData()
    {
        // Line Chart: Reset to empty state
        ProductionSeries = new SeriesCollection();
        ProductionLabels = new string[] { };

        // Pie Chart: Reset to empty state
        DefectSeries = new SeriesCollection();

        // Recent Activities: Clear mock data
        Activities.Clear();
        
        // Reset Stats to real starting values
        MaterialAlerts = "0";
        TodayProductivity = "0%";
        MonthlyRevenue = "0 VNĐ";
    }

    [RelayCommand]
    private void UpdateSelectedDefect(ChartPoint point)
    {
        if (point == null)
        {
            SelectedDefectTitle = "Tổng mẫu";
            SelectedDefectValue = _qcTotalSamples;
            return;
        }

        SelectedDefectTitle = point.SeriesView.Title;
        SelectedDefectValue = point.Y;
    }

    [RelayCommand]
    private void ViewOrderDetails(UrgentOrder order)
    {
        if (order == null || string.IsNullOrEmpty(order.Code)) return;
        
        var vm = _navigationService.NavigateTo<WorkOrderDetailViewModel>();
        if (vm != null)
        {
            _ = vm.LoadOrder(order.Code);
        }
    }



    [RelayCommand]
    private async Task ViewAllActivities()
    {
        var vm = _navigationService.NavigateTo<ActivitiesViewModel>();
        if (vm != null) await vm.LoadInitialDataAsync();
    }

    [RelayCommand]
    private void ExportActivities()
    {
        _navigationService.NavigateTo<ActivityImportExportViewModel>();
    }

    [RelayCommand]
    private void OpenActivityDetail(RecentActivity activity)
    {
        if (activity == null) return;
        
        var vm = _navigationService.NavigateTo<ActivityDetailViewModel>();
        vm.Initialize(activity);
    }

    [RelayCommand]
    private void ViewAllOrders()
    {
        _navigationService.NavigateTo<ProductionViewModel>();
        if (_navigationService.CurrentView is ProductionViewModel productionVm)
        {
            productionVm.ActiveTabIndex = 1; // Tab "Lệnh sản xuất"
        }
    }
}

public class UrgentOrder
{
    public string? Code { get; set; }
    public string? Product { get; set; }
    public int Quantity { get; set; }
    public int Progress { get; set; }
    public string? Status { get; set; }
    public bool IsUrgent { get; set; }
    public string? StartDate { get; set; }
    public string? Deadline { get; set; }
    public string? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class RecentActivity
{
    public string? Type { get; set; }
    public string? Content { get; set; }
    public string? User { get; set; }
    public string? Time { get; set; }
}

public class DefectStatItem
{
    public string Reason { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string PercentageDisplay => $"{Percentage:N1}%";
}
