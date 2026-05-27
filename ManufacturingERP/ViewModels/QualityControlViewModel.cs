using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Services;
using ManufacturingERP.Models;
using LiveCharts;
using LiveCharts.Wpf;

namespace ManufacturingERP.ViewModels;

public partial class QualityControlViewModel : ViewModelBase
{
    private readonly IAccessControlService _accessControlService;
    private readonly INotificationService _notificationService;
    private readonly IQualityControlService _qcService;
    private readonly IAuthService _authService;
    private readonly IProductionService _productionService;

    [ObservableProperty]
    private string _title = "Kiểm soát chất lượng";

    // Dashboard Stats
    [ObservableProperty] private int _totalSamples = 1250;
    [ObservableProperty] private int _passedSamples = 1210;
    [ObservableProperty] private int _failedSamples = 40;
    [ObservableProperty] private int _pendingBatches = 5;

    public string DefectRate => TotalSamples > 0 ? $"{(double)FailedSamples / TotalSamples * 100:F1}%" : "0%";
    public string PassedRate => TotalSamples > 0 ? $"{(double)PassedSamples / TotalSamples * 100:F1}%" : "0%";

    // Chart Data
    public ChartValues<double> DefectTrendValues { get; } = new();
    public ChartValues<int> TotalSamplesTrendValues { get; } = new();
    public ObservableCollection<string> TrendLabels { get; } = new();
    
    [ObservableProperty] private string _selectedTrendPeriod = "Tháng này";
    public string[] TrendPeriods { get; } = { "Hôm nay", "7 ngày qua", "Tháng này", "Tùy chỉnh" };
    
    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;
    [ObservableProperty] private bool _isCustomRangeVisible;

    // Form Properties
    [ObservableProperty] private string _selectedOrderId = string.Empty;
    [ObservableProperty] private int? _selectedProductId;
    [ObservableProperty] private string _orderSearchText = string.Empty;
    [ObservableProperty] private int _passedQtyInput;
    [ObservableProperty] private int _failedQtyInput;
    [ObservableProperty] private string _defectReasonInput = string.Empty;
    [ObservableProperty] private string _inspectorName = "Huy Hoàng";

    [ObservableProperty] private bool _canAddQC;
    [ObservableProperty] private bool _canEditQC;
    [ObservableProperty] private bool _canDeleteQC;

    // Stats for Selected Item
    [ObservableProperty] private int _selectedOrderProducedQty;
    [ObservableProperty] private int _selectedOrderInternalDefects;
    [ObservableProperty] private int _selectedOrderInspectedQty;
    [ObservableProperty] private int _selectedOrderRemainingQty;

    public ObservableCollection<QCRecordDisplay> QCRecords { get; } = new();
    private List<QCRecordDisplay> _allQCRecords = new();
    
    [ObservableProperty] private string _searchText = string.Empty;
    
    public ObservableCollection<DefectCategoryDisplay> DefectStats { get; } = new();
    public ObservableCollection<WorkOrder> AvailableOrders { get; } = new();
    public ObservableCollection<WorkOrder> FilteredAvailableOrders { get; } = new();
    public ObservableCollection<WorkOrderItem> AvailableProducts { get; } = new();

    private void ApplyOrderFilter()
    {
        var filter = OrderSearchText?.ToLower() ?? "";
        App.Current.Dispatcher.Invoke(() => {
            FilteredAvailableOrders.Clear();
            var filtered = AvailableOrders.Where(o => 
                o.Wocode.ToLower().Contains(filter) || 
                (o.WorkOrderItems?.Any(i => i.Product?.MaterialName.ToLower().Contains(filter) ?? false) ?? false)
            ).ToList();
            foreach (var o in filtered) FilteredAvailableOrders.Add(o);
        });
    }

    partial void OnOrderSearchTextChanged(string value) => ApplyOrderFilter();

    partial void OnSelectedOrderIdChanged(string value)
    {
        SelectedProductId = null;
        AvailableProducts.Clear();
        
        var order = AvailableOrders.FirstOrDefault(o => o.Wocode == value);
        if (order != null && order.WorkOrderItems != null)
        {
            foreach (var item in order.WorkOrderItems)
                AvailableProducts.Add(item);
            
            if (AvailableProducts.Count == 1)
                SelectedProductId = AvailableProducts[0].ItemId;
        }
        UpdateSelectedStats();
    }

    partial void OnSelectedProductIdChanged(int? value) => UpdateSelectedStats();

    private void UpdateSelectedStats()
    {
        var order = AvailableOrders.FirstOrDefault(o => o.Wocode == SelectedOrderId);
        if (order != null && SelectedProductId.HasValue)
        {
            var item = order.WorkOrderItems?.FirstOrDefault(i => i.ItemId == SelectedProductId.Value);
            if (item != null)
            {
                SelectedOrderProducedQty = item.ReportedQty;
                SelectedOrderInternalDefects = item.InternalFailedQty;
                SelectedOrderInspectedQty = item.ActualQty + item.QCFailedQty;
                SelectedOrderRemainingQty = Math.Max(0, item.TargetQty - SelectedOrderInspectedQty);
                return;
            }
        }
        
        SelectedOrderProducedQty = 0;
        SelectedOrderInternalDefects = 0;
        SelectedOrderInspectedQty = 0;
        SelectedOrderRemainingQty = 0;
    }

    private async Task LoadPermissionsAsync()
    {
        await _accessControlService.RefreshAsync();
        var perms = ModulePermissionStateFactory.FromAccessControl(_accessControlService, SystemModules.QualityControl);
        CanAddQC = perms.CanAdd;
        CanEditQC = perms.CanEdit;
        CanDeleteQC = perms.CanDelete;
    }



    public QualityControlViewModel(
        IAccessControlService accessControlService, 
        INotificationService notificationService,
        IQualityControlService qcService,
        IAuthService authService,
        IProductionService productionService)
    {
        _accessControlService = accessControlService;
        _notificationService = notificationService;
        _qcService = qcService;
        _authService = authService;
        _productionService = productionService;

        InspectorName = _authService.CurrentUser?.Employee?.FullName ?? "Hệ thống";
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            await LoadPermissionsAsync();
            // 1. Cập nhật ngày dựa trên SelectedTrendPeriod
            if (SelectedTrendPeriod != "Tùy chỉnh")
            {
                EndDate = DateTime.Now;
                StartDate = SelectedTrendPeriod switch {
                    "Hôm nay" => DateTime.Now.Date,
                    "7 ngày qua" => DateTime.Now.AddDays(-7),
                    "Tháng này" => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    _ => DateTime.Now.AddMonths(-1)
                };
            }

            // 2. Tải dữ liệu thống kê theo khoảng thời gian đã chọn
            var stats = await _qcService.GetStatisticsAsync(StartDate, EndDate);
            TotalSamples = stats.Total;
            PassedSamples = stats.Passed;
            FailedSamples = stats.Failed;

            // 3. Tải danh sách QC gần đây (vẫn lấy 20 bản ghi mới nhất không phụ thuộc filter để theo dõi log)
            var records = await _qcService.GetRecentRecordsAsync(20);
            App.Current.Dispatcher.Invoke(() => {
                QCRecords.Clear();
                _allQCRecords.Clear();
                foreach (var r in records)
                {
                    var display = new QCRecordDisplay {
                        Date = r.InspectionDate?.ToString("yyyy-MM-dd") ?? "-",
                        BatchId = r.Wo?.Wocode ?? "N/A",
                        Product = r.Wo?.WorkOrderItems?.FirstOrDefault()?.Product?.MaterialName ?? "Sản phẩm chung",
                        Inspector = r.Inspector?.Employee?.FullName ?? r.Inspector?.Username ?? "Hệ thống",
                        Passed = r.PassedQty ?? 0,
                        Failed = r.FailedQty ?? 0,
                        SamplesUsed = (r.PassedQty ?? 0) + (r.FailedQty ?? 0),
                        Status = (r.FailedQty ?? 0) > ((r.PassedQty ?? 0) + (r.FailedQty ?? 0)) * 0.1 ? "Không đạt" : "Đạt"
                    };
                    QCRecords.Add(display);
                    _allQCRecords.Add(display);
                }
                if (!string.IsNullOrEmpty(SearchText)) ApplyFilter();
            });

            // 4. Tải các lệnh sản xuất chờ kiểm tra
            var pendingOrders = await _qcService.GetPendingInspectionOrdersAsync();
            App.Current.Dispatcher.Invoke(() => {
                AvailableOrders.Clear();
                foreach (var order in pendingOrders) AvailableOrders.Add(order);
                PendingBatches = AvailableOrders.Count;
                ApplyOrderFilter(); // Cập nhật danh sách lọc ban đầu
            });


            // 5. Cập nhật biểu đồ xu hướng dựa trên khoảng cách ngày (Tối ưu số lượng cột để tránh bị dày)
            string periodToQuery = "Ngày";
            int count = 7;
            var diffDays = (EndDate - StartDate).TotalDays;

            if (diffDays <= 7) { periodToQuery = "Ngày"; count = 7; } 
            else if (diffDays <= 31) { periodToQuery = "Ngày"; count = (int)diffDays + 1; }
            else if (diffDays <= 120) { periodToQuery = "Tuần"; count = (int)Math.Ceiling(diffDays / 7); }
            else if (diffDays <= 540) { periodToQuery = "Tháng"; count = (int)Math.Ceiling(diffDays / 30); }
            else { periodToQuery = "Quý"; count = (int)Math.Ceiling(diffDays / 90); }


            var trend = await _qcService.GetDefectTrendAsync(periodToQuery, count, StartDate);
            App.Current.Dispatcher.Invoke(() => {
                DefectTrendValues.Clear();
                TotalSamplesTrendValues.Clear();
                TrendLabels.Clear();
                foreach (var t in trend) 
                {
                    DefectTrendValues.Add(t.DefectRate);
                    TotalSamplesTrendValues.Add(t.TotalSamples);
                    TrendLabels.Add(t.Label);
                }
            });

            OnPropertyChanged(nameof(DefectRate));
            OnPropertyChanged(nameof(PassedRate));
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi tải dữ liệu QC: " + ex.Message);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedTrendPeriodChanged(string value)
    {
        IsCustomRangeVisible = value == "Tùy chỉnh";
        if (value != "Tùy chỉnh") _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task ApplyCustomFilter()
    {
        await LoadDataAsync();
    }

    private void ApplyFilter()
    {
        var filter = SearchText?.ToLower() ?? "";
        
        App.Current.Dispatcher.Invoke(() => {
            QCRecords.Clear();
            var filtered = _allQCRecords.Where(r => 
                r.BatchId.ToLower().Contains(filter) || 
                r.Product.ToLower().Contains(filter) ||
                r.Inspector.ToLower().Contains(filter)
            ).ToList();
            
            foreach (var r in filtered) QCRecords.Add(r);
        });
    }

    [RelayCommand]
    private async Task SaveQCResult()
    {
        if (!_accessControlService.HasCached(SystemModules.QualityControl, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Kiểm soát chất lượng.");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedOrderId))
        {
            _notificationService.ShowError("Vui lòng chọn một Lệnh sản xuất.");
            return;
        }

        if (!SelectedProductId.HasValue)
        {
            _notificationService.ShowError("Vui lòng chọn sản phẩm cần kiểm tra.");
            return;
        }

        if (FailedQtyInput > 0 && string.IsNullOrWhiteSpace(DefectReasonInput))
        {
            _notificationService.ShowError("Vui lòng nhập lý do cho số lượng lỗi.");
            return;
        }

        if (PassedQtyInput < 0 || FailedQtyInput < 0)
        {
            _notificationService.ShowError("Số lượng không thể là số âm.");
            return;
        }

        if (PassedQtyInput == 0 && FailedQtyInput == 0)
        {
            _notificationService.ShowError("Vui lòng nhập số lượng ĐẠT hoặc LỖI.");
            return;
        }

        if (PassedQtyInput + FailedQtyInput > SelectedOrderRemainingQty)
        {
            _notificationService.ShowError($"Số lượng kiểm tra ({PassedQtyInput + FailedQtyInput}) vượt quá số lượng còn lại ({SelectedOrderRemainingQty}).");
            return;
        }

        var order = AvailableOrders.FirstOrDefault(o => o.Wocode == SelectedOrderId);
        if (order == null) return;

        var record = new QualityControl
        {
            Woid = order.Woid,
            WorkOrderItemId = SelectedProductId.Value,
            PassedQty = PassedQtyInput,
            FailedQty = FailedQtyInput,
            InspectorId = _authService.CurrentUser?.UserId,
            InspectionDate = DateTime.Now,
            DefectReason = FailedQtyInput > 0 ? DefectReasonInput : "Đạt chuẩn"
        };


        try 
        {
            var success = await _qcService.AddRecordAsync(record);
            if (success)
            {
                double total = (record.PassedQty ?? 0) + (record.FailedQty ?? 0);
                double rate = total > 0 ? (double)(record.FailedQty ?? 0) / total : 0;

                if (rate > 0.1)
                {
                    _notificationService.ShowError($"CẢNH BÁO: Tỷ lệ lỗi quá cao ({rate:P1})! Hệ thống đã tự động TẠM DỪNG lệnh {SelectedOrderId} để kiểm tra.");
                }
                else
                {
                    _notificationService.ShowSuccess($"Đã lưu kết quả QC cho lệnh {SelectedOrderId}.");
                }
                
                // Reset Form
                PassedQtyInput = 0;
                FailedQtyInput = 0;
                DefectReasonInput = string.Empty;
                SelectedOrderId = string.Empty;

                // Reload Data
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowError("Có lỗi xảy ra khi lưu kết quả QC.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(ex.Message);
        }

    }
}

public class QCRecordDisplay
{
    public string Date { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string Inspector { get; set; } = string.Empty;
    public int SamplesUsed { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public string Status { get; set; } = string.Empty;

    public double DefectRateValue => (Passed + Failed) > 0 ? (double)Failed / (Passed + Failed) : 0;
    public string DefectRateDisplay => (Passed + Failed) > 0 ? $"{DefectRateValue * 100:F1}%" : "0%";
}

public class DefectCategoryDisplay
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
