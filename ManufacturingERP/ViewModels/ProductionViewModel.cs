using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using ManufacturingERP.Core;
using ManufacturingERP.Services;

using GongSolutions.Wpf.DragDrop;

namespace ManufacturingERP.ViewModels;

public partial class ProductionViewModel : ViewModelBase, IDropTarget
{
    [ObservableProperty]
    private string _title = "Quản lý sản xuất";

    [ObservableProperty]
    private int _activeTabIndex = 0;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatusFilter = "Tất cả";

    partial void OnSearchTextChanged(string value) => _ = LoadDataAsync();
    partial void OnSelectedStatusFilterChanged(string value) => _ = LoadDataAsync();

    [ObservableProperty] private bool _isAdvancedFilterVisible;
    [ObservableProperty] private DateTime? _filterFromDate;
    [ObservableProperty] private DateTime? _filterToDate;
    [ObservableProperty] private string _activityLogSearchText = string.Empty;
    [ObservableProperty] private string _activityLogSelectedStatus = "Tất cả";
    partial void OnActivityLogSearchTextChanged(string value) => _ = LoadActivityLogsAsync();
    partial void OnActivityLogSelectedStatusChanged(string value) => _ = LoadActivityLogsAsync();
    [ObservableProperty] private bool _canAddProduction;
    [ObservableProperty] private bool _canEditProduction;
    [ObservableProperty] private bool _canDeleteProduction;

    public ObservableCollection<WorkOrderDisplay> WorkOrders { get; } = new();
    public ObservableCollection<KanbanItem> KanbanOrders { get; } = new();
    public ObservableCollection<ActivityLog> ActivityLogs { get; } = new();

    // Filtered collections for Kanban columns
    public IEnumerable<KanbanItem> PendingOrders => KanbanOrders.Where(x => x.Status == "Chờ duyệt");
    public IEnumerable<KanbanItem> InProductionOrders => KanbanOrders.Where(x => x.Status == "Đang sản xuất");
    public IEnumerable<KanbanItem> PausedOrders => KanbanOrders.Where(x => x.Status == "Tạm dừng");
    public IEnumerable<KanbanItem> CompletedOrders => KanbanOrders.Where(x => x.Status == "Hoàn thành");
    public IEnumerable<KanbanItem> CancelledOrders => KanbanOrders.Where(x => x.Status == "Đã hủy");

    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IAccessControlService _accessControlService;
    private readonly IProductionService _productionService;
    private readonly INavigationService _navigationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAuthService _authService;

    public ProductionViewModel(
        IFileService fileService, 
        INotificationService notificationService, 
        IAccessControlService accessControlService,
        IProductionService productionService,
        INavigationService navigationService,
        IAuditLogService auditLogService,
        IAuthService authService)
    {
        _fileService = fileService;
        _notificationService = notificationService;
        _accessControlService = accessControlService;
        _productionService = productionService;
        _navigationService = navigationService;
        _auditLogService = auditLogService;
        _authService = authService;
        
        _ = InitializePermissionsAndDataAsync();
    }

    private async Task InitializePermissionsAndDataAsync()
    {
        await LoadPermissionsAsync();
        await LoadActivityLogsAsync();
        await LoadDataAsync();
    }

    private async Task LoadPermissionsAsync()
    {
        await _accessControlService.RefreshAsync();
        var perms = ModulePermissionStateFactory.FromAccessControl(_accessControlService, SystemModules.Production);
        CanAddProduction = perms.CanAdd;
        CanEditProduction = perms.CanEdit;
        CanDeleteProduction = perms.CanDelete;
    }

    private bool EnsureCanEditProduction()
    {
        if (_accessControlService.HasCached(SystemModules.Production, PermissionAction.Edit))
        {
            return true;
        }

        _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Sản xuất.");
        return false;
    }

    [RelayCommand]
    private async Task AddWorkOrder()
    {
        if (!_accessControlService.HasCached(SystemModules.Production, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Sản xuất.");
            return;
        }

        var vm = _navigationService.NavigateTo<CreateWorkOrderViewModel>();
        await vm.InitializeAsync();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private void ToggleAdvancedSearch()
    {
        IsAdvancedFilterVisible = !IsAdvancedFilterVisible;
    }

    partial void OnFilterFromDateChanged(DateTime? value) => _ = LoadDataAsync();
    partial void OnFilterToDateChanged(DateTime? value) => _ = LoadDataAsync();

    [RelayCommand]
    private async Task ExportToExcel()
    {
        if (!await _accessControlService.HasAsync(SystemModules.Production, PermissionAction.View))
        {
            _notificationService.ShowError("Bạn không có quyền Xem trong phân hệ Sản xuất.");
            return;
        }

        _navigationService.NavigateTo<ImportExportViewModel>();
    }

    public async Task LoadDataAsync()
    {
        try
        {
            await LoadPermissionsAsync();
            var today = DateTime.Today;

            DateTime startDate, endDate;
            if (IsAdvancedFilterVisible && FilterFromDate.HasValue && FilterToDate.HasValue)
            {
                startDate = FilterFromDate.Value;
                endDate = FilterToDate.Value;
            }
            else
            {
                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = startDate.AddMonths(1).AddDays(-1);
            }

            var dbOrders = await _productionService.GetFilteredWorkOrdersAsync(startDate, endDate, null);
            
            // If no data, fallback to recent
            if (dbOrders == null || !dbOrders.Any())
            {
                dbOrders = await _productionService.GetRecentWorkOrdersAsync(50);
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                WorkOrders.Clear();
                KanbanOrders.Clear();
                
                int passedTotal = 0;
                int targetTotal = 0;

                foreach (var wo in dbOrders)
                {
                    var product = wo.WorkOrderItems?.FirstOrDefault()?.Product?.MaterialName ?? "Sản phẩm chung";
                    var reportedQty = wo.WorkOrderProgresses?
                        .Where(p => p.StageName != "Kiểm tra chất lượng (QC)" && p.StageName != "CẢNH BÁO HỆ THỐNG")
                        .Sum(p => p.ProducedQty ?? 0) ?? 0;

                    var viStatus = MapStatusToVietnamese(wo.Status ?? "Planned");

                    // Filter logic
                    if (SelectedStatusFilter != "Tất cả" && viStatus != SelectedStatusFilter) continue;
                    if (!string.IsNullOrWhiteSpace(SearchText) && 
                        !wo.Wocode.ToLower().Contains(SearchText.ToLower()) && 
                        !product.ToLower().Contains(SearchText.ToLower())) continue;

                    var isOverdue = wo.EndDate.HasValue && wo.EndDate.Value < DateTime.Today && wo.Status != "Completed" && wo.Status != "Hoàn thành";
                    
                    var targetQty = wo.WorkOrderItems?.Sum(i => i.TargetQty) ?? 0;
                    var actualQty = wo.WorkOrderItems?.Sum(i => i.ActualQty) ?? 0;
                    
                    int failedQty = wo.WorkOrderProgresses?.Sum(p => p.DefectQty ?? 0) ?? 0; 
                    
                    var woDisplay = new WorkOrderDisplay
                    {
                        Id = wo.Wocode,
                        Product = product,
                        TargetQty = targetQty,
                        PassedQty = actualQty,
                        FailedQty = failedQty,
                        Status = viStatus,
                        StartDate = wo.StartDate?.ToString("yyyy-MM-dd") ?? "",
                        EndDate = wo.EndDate?.ToString("yyyy-MM-dd") ?? "",
                        IsOverdue = isOverdue,
                        IsHighDefect = false
                    };
                    
                    WorkOrders.Add(woDisplay);

                    KanbanOrders.Add(new KanbanItem
                    {
                        OrderId = wo.Wocode,
                        Title = product,
                        Status = viStatus,
                        TargetQty = targetQty,
                        PassedQty = actualQty,
                        ReportedQty = (int)reportedQty,
                        FailedQty = failedQty,
                        StartDate = wo.StartDate?.ToString("yyyy-MM-dd") ?? "",
                        EndDate = wo.EndDate?.ToString("yyyy-MM-dd") ?? "",
                        IsOverdue = isOverdue
                    });

                    passedTotal += actualQty;
                    targetTotal += targetQty;
                }

                RefreshKanban();
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi tải dữ liệu sản xuất: " + ex.Message);
        }
    }

    private string MapStatusToVietnamese(string dbStatus)
    {
        return dbStatus switch
        {
            "Planned" => "Chờ duyệt",
            "Running" => "Đang sản xuất",
            "Paused" => "Tạm dừng",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            "Chờ" => "Chờ duyệt",
            _ => "Chờ duyệt"
        };
    }
    
    private string MapVietnameseToDbStatus(string viStatus)
    {
        return viStatus switch
        {
            "Chờ duyệt" => "Planned",
            "Đang sản xuất" => "Running",
            "Tạm dừng" => "Paused",
            "Hoàn thành" => "Completed",
            "Đã hủy" => "Cancelled",
            _ => "Planned"
        };
    }

    private async Task LoadActivityLogsAsync()
    {
        try
        {
            App.Current.Dispatcher.Invoke(() => ActivityLogs.Clear());
            
            // Lấy dữ liệu tiến độ thực tế từ bảng WorkOrderProgress
            var progressList = await _productionService.GetRecentProgressAsync(50);
            
            foreach (var prog in progressList)
            {
                var orderCode = prog.Wo?.Wocode ?? "-";
                var stage = prog.StageName ?? "Sản xuất";
                var operatorName = prog.RecordedBy ?? "Công nhân";

                // Filter
                if (!string.IsNullOrEmpty(ActivityLogSearchText))
                {
                    var search = ActivityLogSearchText.ToLower();
                    if (!orderCode.ToLower().Contains(search) &&
                        !operatorName.ToLower().Contains(search) &&
                        !stage.ToLower().Contains(search))
                        continue;
                }
                if (ActivityLogSelectedStatus != "Tất cả" && stage != ActivityLogSelectedStatus)
                    continue;

                var activity = new ActivityLog { 
                    Time = prog.EndTime?.ToString("HH:mm:ss") ?? prog.StartTime?.ToString("HH:mm:ss") ?? "", 
                    Date = prog.EndTime?.ToString("yyyy-MM-dd") ?? prog.StartTime?.ToString("yyyy-MM-dd") ?? "", 
                    Status = "Tiến độ",
                    OrderId = orderCode,
                    Product = "-",
                    Stage = stage,
                    Operator = operatorName,
                    Quantity = prog.ProducedQty?.ToString() ?? "0",
                    Notes = prog.Notes ?? ""
                };

                App.Current.Dispatcher.Invoke(() => ActivityLogs.Add(activity));
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Không thể tải nhật ký hoạt động: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task StartOrder(KanbanItem item)
    {
        if (!EnsureCanEditProduction()) return;
        await UpdateOrderStatusAsync(item, "Đang sản xuất");
    }

    [RelayCommand]
    private async Task PauseOrder(KanbanItem item)
    {
        if (!EnsureCanEditProduction()) return;
        await UpdateOrderStatusAsync(item, "Tạm dừng");
    }

    [RelayCommand]
    private async Task CompleteOrder(KanbanItem item)
    {
        if (!EnsureCanEditProduction()) return;
        await UpdateOrderStatusAsync(item, "Hoàn thành");
    }

    [RelayCommand]
    private void ViewOrderDetails(object parameter)
    {
        string wocode = "";
        if (parameter is WorkOrderDisplay display) wocode = display.Id;
        else if (parameter is KanbanItem kanban) wocode = kanban.OrderId;
        
        if (string.IsNullOrEmpty(wocode)) return;

        var detailVM = _navigationService.NavigateTo<WorkOrderDetailViewModel>();
        detailVM.LoadOrder(wocode);
    }

    private async Task UpdateOrderStatusAsync(KanbanItem item, string newViStatus)
    {
        if (item == null) return;
        if (!EnsureCanEditProduction()) return;

        if (newViStatus == "Đang sản xuất")
        {
            var dbOrder = await _productionService.GetWorkOrderByCodeAsync(item.OrderId);
            if (dbOrder != null)
            {
                var shortages = await _productionService.CheckMaterialAvailabilityAsync(dbOrder.Woid);
                var missing = shortages.Where(s => !s.IsEnough).ToList();
                
                if (missing.Any())
                {
                    string msg = "KHÔNG ĐỦ NGUYÊN LIỆU ĐỂ SẢN XUẤT:\n";
                    foreach (var m in missing)
                    {
                        msg += $"- {m.MaterialName}: Thiếu {m.Shortage:N0} {m.Unit} (Cần {m.RequiredQty:N0}, Có {m.AvailableQty:N0})\n";
                    }
                    _notificationService.ShowError(msg);
                    return;
                }
            }
        }

        if (newViStatus == "Hoàn thành")
        {
            if (item.TargetQty == 0 || item.PassedQty < item.TargetQty)
            {
                _notificationService.ShowError($"Không thể hoàn thành lệnh {item.OrderId}! Tiến độ ({item.PassedQty}/{item.TargetQty}) chưa đạt 100%.");
                return;
            }
        }
        
        try
        {
            var dbOrder = await _productionService.GetWorkOrderByCodeAsync(item.OrderId);
            if (dbOrder != null)
            {
                dbOrder.Status = MapVietnameseToDbStatus(newViStatus);
                var success = await _productionService.UpdateWorkOrderAsync(dbOrder);
                if (success)
                {
                    await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, newViStatus, "WorkOrders", null, $"Lệnh {dbOrder.Wocode}");
                    item.Status = newViStatus;
                    RefreshKanban();
                    
                    var woDisplay = WorkOrders.FirstOrDefault(w => w.Id == item.OrderId);
                    if (woDisplay != null)
                    {
                        woDisplay.Status = newViStatus;
                    }
                    
                    _notificationService.ShowSuccess($"Đã chuyển lệnh {item.OrderId} sang trạng thái {newViStatus}.");
                    await LoadActivityLogsAsync();
                }
                else
                {
                    _notificationService.ShowError("Có lỗi xảy ra khi lưu trạng thái xuống CSDL.");
                }
            }
            else
            {
                _notificationService.ShowError("Không tìm thấy lệnh sản xuất trong CSDL.");
            }
        }
        catch (Exception ex)
        {
             _notificationService.ShowError("Lỗi hệ thống: " + ex.Message);
        }
    }

    // Counts for column headers
    public int PendingCount => KanbanOrders.Count(x => x.Status == "Chờ duyệt");
    public int InProductionCount => KanbanOrders.Count(x => x.Status == "Đang sản xuất");
    public int PausedCount => KanbanOrders.Count(x => x.Status == "Tạm dừng");
    public int CompletedCount => KanbanOrders.Count(x => x.Status == "Hoàn thành");
    public int CancelledCount => KanbanOrders.Count(x => x.Status == "Đã hủy");


    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is KanbanItem && dropInfo.VisualTarget is System.Windows.FrameworkElement)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = System.Windows.DragDropEffects.Move;
        }
    }

    public async void Drop(IDropInfo dropInfo)
    {
        try
        {
            if (!EnsureCanEditProduction()) return;

            if (dropInfo.Data is KanbanItem item && dropInfo.VisualTarget is System.Windows.FrameworkElement targetElement)
            {
                string? newStatus = targetElement.Tag as string;
                if (!string.IsNullOrEmpty(newStatus) && item.Status != newStatus)
                {
                    await UpdateOrderStatusAsync(item, newStatus);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drop error: {ex.Message}");
            _notificationService.ShowError($"Lỗi khi cập nhật trạng thái: {ex.Message}");
        }
    }

    private void RefreshKanban()
    {
        // Trigger UI updates for filtered collections and counts
        OnPropertyChanged(nameof(PendingOrders));
        OnPropertyChanged(nameof(InProductionOrders));
        OnPropertyChanged(nameof(PausedOrders));
        OnPropertyChanged(nameof(CompletedOrders));
        OnPropertyChanged(nameof(CancelledOrders));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(InProductionCount));
        OnPropertyChanged(nameof(PausedCount));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(CancelledCount));

    }
}

public partial class KanbanItem : ObservableObject
{
    [ObservableProperty] private string _orderId = "";
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _status = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(DefectRate))]
    private int _passedQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _reportedQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _targetQty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DefectRate))]
    private int _failedQty;

    [ObservableProperty] private string _startDate = "";
    [ObservableProperty] private string _endDate = "";
    [ObservableProperty] private bool _isOverdue;

    public int Progress => TargetQty > 0 ? (int)((double)PassedQty / TargetQty * 100) : 0;

    
    public string DefectRate 
    {
        get 
        {
            var total = PassedQty + FailedQty;
            if (total == 0) return "0%";
            return $"{(double)FailedQty / total * 100:F1}%";
        }
    }
}

public partial class WorkOrderDisplay : ObservableObject
{
    public string Id { get; set; } = "";
    public string Product { get; set; } = "";
    public int PassedQty { get; set; }
    public int TargetQty { get; set; }
    public int FailedQty { get; set; }
    [ObservableProperty] private string _status = "";
    public string StartDate { get; set; } = "";
    public string EndDate { get; set; } = "";
    public bool IsOverdue { get; set; }
    public bool IsHighDefect { get; set; }

    public int Progress => TargetQty > 0 ? (int)((double)PassedQty / TargetQty * 100) : 0;
}

public class ActivityLog
{
    public string Time { get; set; } = "";
    public string Date { get; set; } = "";
    public string Status { get; set; } = ""; // Tiến hành, Hoàn thành
    public string OrderId { get; set; } = "";
    public string Product { get; set; } = "";
    public string Stage { get; set; } = "";
    public string Operator { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string Notes { get; set; } = "";
}
