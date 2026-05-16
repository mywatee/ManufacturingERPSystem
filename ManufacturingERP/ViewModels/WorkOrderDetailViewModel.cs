using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;
public partial class WorkerProgressItem : ObservableObject
{
    [ObservableProperty] private User _user;
    [ObservableProperty] private DateTime _startTime;
    [ObservableProperty] private DateTime _endTime;

    public WorkerProgressItem(User user, DateTime start, DateTime end)
    {
        _user = user;
        _startTime = start;
        _endTime = end;
    }
}


public partial class WorkOrderDetailViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IProductionService _productionService;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAuthService _authService;
    private readonly IMasterDataService _masterDataService;
    private readonly IUserManagementService _userManagementService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalTargetQty))]
    [NotifyPropertyChangedFor(nameof(TotalActualQty))]
    [NotifyPropertyChangedFor(nameof(TotalReportedQty))]
    [NotifyPropertyChangedFor(nameof(StatusVi))]
    [NotifyPropertyChangedFor(nameof(DateRangeText))]
    [NotifyPropertyChangedFor(nameof(UrgentText))]
    [NotifyPropertyChangedFor(nameof(UrgentColor))]
    private WorkOrder? _order;


    [ObservableProperty]
    private bool _isLoading;

    // === Inline Edit Panel ===
    [ObservableProperty] private bool _isEditPanelOpen;
    [ObservableProperty] private ObservableCollection<Material> _productList = new();
    [ObservableProperty] private Material? _editSelectedProduct;
    [ObservableProperty] private string _editQuantity = "";
    [ObservableProperty] private DateTime? _editEndDate;
    [ObservableProperty] private bool _editIsUrgent;

    // === Inline Record Progress Panel ===
    [ObservableProperty] private bool _isRecordPanelOpen;
    [ObservableProperty] private WorkOrderItem? _recordSelectedProduct;
    [ObservableProperty] private string _recordActualQty = "0";
    [ObservableProperty] private string _recordFailedQty = "0";
    [ObservableProperty] private string _recordSelectedStage = "Gia công / Cắt gọt";
    [ObservableProperty] private string _recordOperator = "Hệ thống";
    [ObservableProperty] private string _recordNotes = "";
    [ObservableProperty] private ObservableCollection<string> _stageOptions = new();
    
    [ObservableProperty] private ObservableCollection<User> _workerList = new();
    [ObservableProperty] private User? _selectedWorkerToAdd;
    [ObservableProperty] private ObservableCollection<WorkerProgressItem> _recordWorkerList = new();
    [ObservableProperty] private DateTime _recordStartTime = DateTime.Now.AddHours(-1);
    [ObservableProperty] private DateTime _recordEndTime = DateTime.Now;

    private readonly List<string> _defaultStages = new()
    {
        "Gia công / Cắt gọt",
        "Lắp ráp / Hàn",
        "Sơn phủ / Xử lý bề mặt",
        "Kiểm tra chất lượng (QC)",
        "Đóng gói / Hoàn thiện",
        "Xuất kho"
    };

    public string CreatorName => Order?.CreatedByNavigation?.Employee?.FullName ?? Order?.CreatedByNavigation?.Username ?? "N/A";
    
    public string StatusVi => Order?.Status switch
    {
        "Planned" => "Chờ duyệt",
        "Running" => "Đang sản xuất",
        "Paused" => "Tạm dừng",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        "Chờ" => "Chờ duyệt",
        _ => "Chờ duyệt"
    };

    public string DateRangeText => Order != null ? $"{(Order.StartDate.HasValue ? Order.StartDate.Value.ToString("dd/MM/yyyy") : "N/A")} - {(Order.EndDate.HasValue ? Order.EndDate.Value.ToString("dd/MM/yyyy") : "N/A")}" : "";

    public string UrgentText => (Order?.IsUrgent ?? false) ? "Khẩn cấp" : "Bình thường";
    public Brush UrgentColor => (Order?.IsUrgent ?? false) ? new SolidColorBrush(Color.FromRgb(220, 38, 38)) : new SolidColorBrush(Color.FromRgb(22, 163, 74));

    public bool CanStart => Order?.Status == "Planned" || Order?.Status == "Chờ duyệt" || Order?.Status == "Chờ" || Order?.Status == "Paused" || Order?.Status == "Tạm dừng";
    public bool CanPause => Order?.Status == "Running" || Order?.Status == "Đang sản xuất";
    public bool CanComplete => Order?.Status == "Running" || Order?.Status == "Đang sản xuất" || Order?.Status == "Paused" || Order?.Status == "Tạm dừng";
    public bool CanEdit => Order?.Status == "Planned" || Order?.Status == "Chờ duyệt" || Order?.Status == "Chờ";
    public bool CanCancel => Order?.Status == "Planned" || Order?.Status == "Chờ duyệt" || Order?.Status == "Chờ" || Order?.Status == "Paused" || Order?.Status == "Tạm dừng" || Order?.Status == "Running" || Order?.Status == "Đang sản xuất";
    public bool CanRecord => Order?.Status == "Running" || Order?.Status == "Đang sản xuất";

    public int TotalTargetQty => Order?.WorkOrderItems?.Sum(i => i.TargetQty) ?? 0;
    public int TotalActualQty => Order?.WorkOrderItems?.Sum(i => i.ActualQty) ?? 0;
    public int TotalReportedQty => Order?.WorkOrderItems?.Sum(i => i.ReportedQty) ?? 0;


    public WorkOrderDetailViewModel(
        INavigationService navigationService,
        IProductionService productionService,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        IAuthService authService,
        IMasterDataService masterDataService,
        IUserManagementService userManagementService)
    {
        _navigationService = navigationService;
        _productionService = productionService;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _authService = authService;
        _masterDataService = masterDataService;
        _userManagementService = userManagementService;
        
        // Load default stages initially
        foreach (var stage in _defaultStages) StageOptions.Add(stage);
    }

    public async Task LoadOrder(string orderId)
    {
        IsLoading = true;
        try
        {
            var fullOrder = await _productionService.GetWorkOrderByCodeAsync(orderId);
            if (fullOrder != null)
            {
                Order = fullOrder;
                OnPropertyChanged(nameof(CreatorName));
                OnPropertyChanged(nameof(StatusVi));
                OnPropertyChanged(nameof(DateRangeText));
                OnPropertyChanged(nameof(UrgentText));
                OnPropertyChanged(nameof(UrgentColor));
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanComplete));
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(CanRecord));
                OnPropertyChanged(nameof(TotalTargetQty));
                OnPropertyChanged(nameof(TotalActualQty));
            }
            else
            {
                _notificationService.ShowError("Không tìm thấy dữ liệu lệnh sản xuất.");
                GoBack();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi khi tải chi tiết lệnh: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        var vm = _navigationService.NavigateTo<ProductionViewModel>();
        await vm.LoadDataAsync();
    }

    // ==================== INLINE EDIT PANEL ====================
    [RelayCommand]
    private async Task EditOrder()
    {
        if (Order == null) return;
        
        var products = await _productionService.GetProductsAsync();
        ProductList.Clear();
        foreach (var p in products) ProductList.Add(p);
        
        EditSelectedProduct = ProductList.FirstOrDefault(p => p.MaterialId == Order.ProductId);
        EditQuantity = (Order.WorkOrderItems?.Sum(i => i.TargetQty) ?? 0).ToString();
        EditEndDate = Order.EndDate;
        EditIsUrgent = Order.IsUrgent;
        
        IsEditPanelOpen = true;
    }

    [RelayCommand]
    private async Task SaveEdit()
    {
        if (Order == null) return;

        if (!int.TryParse(EditQuantity, out int qty) || qty <= 0)
        {
            _notificationService.ShowError("Số lượng kế hoạch phải lớn hơn 0.");
            return;
        }

        Order.TargetQty = qty;
        Order.EndDate = EditEndDate;
        Order.IsUrgent = EditIsUrgent;
        
        if (EditSelectedProduct != null)
        {
            Order.ProductId = EditSelectedProduct.MaterialId;
        }
        
        if (await _productionService.UpdateWorkOrderAsync(Order))
        {
            await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, "Sửa", "WorkOrders", null, $"Lệnh {Order.Wocode}");
            _notificationService.ShowSuccess("Đã cập nhật lệnh sản xuất.");
            IsEditPanelOpen = false;
            await LoadOrder(Order.Wocode);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditPanelOpen = false;
    }

    // ==================== INLINE RECORD PROGRESS PANEL ====================
    [RelayCommand]
    private void RecordProgress()
    {
        if (Order == null) return;
        
        RecordSelectedProduct = Order.WorkOrderItems?.FirstOrDefault();
        RecordActualQty = "0";
        RecordFailedQty = "0";
        RecordSelectedStage = "Gia công / Cắt gọt";
        RecordOperator = _authService.CurrentUser?.Employee?.FullName ?? _authService.CurrentUser?.Username ?? "Hệ thống";
        RecordNotes = "";
        
        RecordStartTime = DateTime.Now.AddHours(-1);
        RecordEndTime = DateTime.Now;
        RecordWorkerList.Clear();
        
        // Load worker list if not loaded
        if (!WorkerList.Any())
        {
            _ = LoadWorkers();
        }
        else
        {
            var currentUser = WorkerList.FirstOrDefault(u => u.UserId == _authService.CurrentUser?.UserId);
            if (currentUser != null)
            {
                RecordWorkerList.Add(new WorkerProgressItem(currentUser, RecordStartTime, RecordEndTime));
            }
        }
        
        IsRecordPanelOpen = true;
    }

    [RelayCommand]
    private void AddWorkerToRecord()
    {
        if (SelectedWorkerToAdd == null) return;
        if (RecordWorkerList.Any(w => w.User.UserId == SelectedWorkerToAdd.UserId)) return;
        
        RecordWorkerList.Add(new WorkerProgressItem(SelectedWorkerToAdd, RecordStartTime, RecordEndTime));
        SelectedWorkerToAdd = null;
    }

    [RelayCommand]
    private void RemoveWorkerFromRecord(WorkerProgressItem item)
    {
        RecordWorkerList.Remove(item);
    }

    private async Task LoadWorkers()
    {
        try
        {
            var users = await _userManagementService.GetUsersAsync();
            WorkerList.Clear();
            foreach (var u in users) WorkerList.Add(u);
            
            var currentUser = WorkerList.FirstOrDefault(u => u.UserId == _authService.CurrentUser?.UserId);
            if (currentUser != null && !RecordWorkerList.Any())
            {
                RecordWorkerList.Add(new WorkerProgressItem(currentUser, RecordStartTime, RecordEndTime));
            }
        }
        catch { }
    }

    partial void OnRecordSelectedProductChanged(WorkOrderItem? value)
    {
        _ = LoadStagesForProduct(value);
    }

    private async Task LoadStagesForProduct(WorkOrderItem? item)
    {
        StageOptions.Clear();
        
        if (item?.Product?.MaterialCode != null)
        {
            try 
            {
                var routings = await _masterDataService.GetRoutingByParentCodeAsync(item.Product.MaterialCode);
                if (routings != null && routings.Any())
                {
                    foreach (var step in routings.OrderBy(r => r.StepNumber))
                    {
                        if (!string.IsNullOrEmpty(step.StepName))
                            StageOptions.Add(step.StepName);
                    }
                    
                    RecordSelectedStage = StageOptions.FirstOrDefault() ?? "N/A";
                    return;
                }
            }
            catch { /* Fallback to default */ }
        }

        // Nếu không có quy trình, hiện thông báo rõ ràng và không cho phép ghi nhận lung tung
        StageOptions.Add("(Chưa thiết lập quy trình)");
        RecordSelectedStage = "(Chưa thiết lập quy trình)";
    }



    [RelayCommand]
    private async Task SaveRecord()
    {
        if (Order == null) return;

        if (!int.TryParse(RecordActualQty, out int actualQty)) actualQty = 0;
        if (!int.TryParse(RecordFailedQty, out int failedQty)) failedQty = 0;

        if (RecordSelectedStage == "(Chưa thiết lập quy trình)" || string.IsNullOrEmpty(RecordSelectedStage))
        {
            _notificationService.ShowError("Vui lòng thiết lập quy trình công nghệ cho sản phẩm này trong mục 'Dữ liệu gốc' trước khi ghi nhận tiến độ.");
            return;
        }

        if (actualQty <= 0 && failedQty <= 0)


        // Kiểm tra vượt định mức
        if (actualQty > 0)
        {
            if (RecordSelectedProduct != null)
            {
                if (RecordSelectedProduct.ActualQty + actualQty > RecordSelectedProduct.TargetQty)
                {
                    bool result = _notificationService.Confirm(
                        $"Bạn đang ghi nhận số lượng ĐẠT là {actualQty}, làm cho tổng số lượng đạt ({RecordSelectedProduct.ActualQty + actualQty}) vượt quá mức mục tiêu ({RecordSelectedProduct.TargetQty}) của sản phẩm này.\n\nBạn có chắc chắn muốn ghi nhận vượt định mức (overproduction) không?", 
                        "Cảnh báo vượt định mức");
                    
                    if (!result) return;
                }
            }
            else
            {
                int currentTotal = Order.WorkOrderItems?.Sum(i => i.ActualQty) ?? 0;
                int targetTotal = Order.WorkOrderItems?.Sum(i => i.TargetQty) ?? 0;
                if (currentTotal + actualQty > targetTotal)
                {
                    bool result = _notificationService.Confirm(
                        $"Bạn đang ghi nhận số lượng ĐẠT là {actualQty}, làm cho tổng số lượng đạt của toàn lệnh ({currentTotal + actualQty}) vượt quá mức mục tiêu ({targetTotal}).\n\nBạn có chắc chắn muốn ghi nhận vượt định mức không?",
                        "Cảnh báo vượt định mức");
                    
                    if (!result) return;
                }
            }
        }

        if (RecordWorkerList.Count == 0)
        {
            _notificationService.ShowError("Vui lòng thêm ít nhất một nhân viên thực hiện.");
            return;
        }

        bool success = true;
        int workerCount = RecordWorkerList.Count;
        
        // Chia đều sản lượng cho các nhân viên (nếu cần theo dõi năng suất cá nhân)
        // Hoặc để nguyên sản lượng tổng nếu chỉ muốn theo dõi chi phí nhân công.
        // Ở đây ta chia đều để tổng ActualQty trong Progress không bị nhân lên vô lý.
        int splitActual = actualQty / workerCount;
        int splitFailed = failedQty / workerCount;
        int remainingActual = actualQty % workerCount;
        int remainingFailed = failedQty % workerCount;

        for (int i = 0; i < workerCount; i++)
        {
            var workerItem = RecordWorkerList[i];
            var progress = new WorkOrderProgress
            {
                Woid = Order.Woid,
                WorkOrderItemId = RecordSelectedProduct?.ItemId,
                ProducedQty = splitActual + (i == 0 ? remainingActual : 0),
                DefectQty = splitFailed + (i == 0 ? remainingFailed : 0),
                StageName = RecordSelectedStage,
                RecordedBy = RecordOperator,
                Notes = RecordNotes,
                WorkerId = workerItem.User.UserId,
                StartTime = workerItem.StartTime,
                EndTime = workerItem.EndTime
            };

            if (!await _productionService.AddWorkOrderProgressAsync(progress))
            {
                success = false;
            }
        }
        
        if (success)
        {
            string logDetail = $"[Tiến độ Nhóm] Stage: {RecordSelectedStage} | Workers: {workerCount} | Total Qty: {actualQty} | WO: {Order.Wocode}";
            await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, "Tiến độ", "WorkOrders", null, logDetail);
            
            _notificationService.ShowSuccess($"Đã ghi nhận tiến độ cho nhóm {workerCount} nhân viên.");
            IsRecordPanelOpen = false;
            await LoadOrder(Order.Wocode);
        }
        else
        {
            _notificationService.ShowError("Có lỗi xảy ra khi lưu một số bản ghi.");
        }
    }

    [RelayCommand]
    private void CancelRecord()
    {
        IsRecordPanelOpen = false;
    }

    // ==================== STATUS COMMANDS ====================
    [RelayCommand]
    private async Task StartOrder()
    {
        if (Order == null) return;
        Order.Status = "Running";
        if (await _productionService.UpdateWorkOrderAsync(Order))
        {
            await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, "Bắt đầu", "WorkOrders", null, $"Lệnh {Order.Wocode}");
            _notificationService.ShowSuccess($"Đã bắt đầu sản xuất lệnh {Order.Wocode}");
            await LoadOrder(Order.Wocode);
        }
        else
        {
            _notificationService.ShowError("Lỗi cập nhật CSDL.");
        }
    }

    [RelayCommand]
    private async Task PauseOrder()
    {
        if (Order == null) return;
        Order.Status = "Paused";
        if (await _productionService.UpdateWorkOrderAsync(Order))
        {
            await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, "Tạm dừng", "WorkOrders", null, $"Lệnh {Order.Wocode}");
            _notificationService.ShowSuccess($"Đã tạm dừng lệnh {Order.Wocode}");
            await LoadOrder(Order.Wocode);
        }
        else
        {
            _notificationService.ShowError("Lỗi cập nhật CSDL.");
        }
    }

    [RelayCommand]
    private async Task CompleteOrder()
    {
        if (Order == null) return;

        int target = TotalTargetQty;
        int actual = TotalActualQty;

        if (target == 0 || actual < target)
        {
            _notificationService.ShowError($"Không thể hoàn thành! Tiến độ ({actual}/{target}) chưa đạt 100%.");
            return;
        }

        Order.Status = "Completed";
        if (await _productionService.UpdateWorkOrderAsync(Order))
        {
            await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, "Hoàn thành", "WorkOrders", null, $"Lệnh {Order.Wocode}");
            _notificationService.ShowSuccess($"Đã hoàn thành lệnh {Order.Wocode}");
            await LoadOrder(Order.Wocode);
        }
        else
        {
            _notificationService.ShowError("Lỗi cập nhật CSDL.");
        }
    }

    [RelayCommand]
    private async Task CancelOrder()
    {
        if (Order == null) return;

        bool confirm = _notificationService.Confirm(
            $"Bạn có chắc chắn muốn hủy lệnh sản xuất {Order.Wocode}? Thao tác này không thể hoàn tác.",
            "Xác nhận hủy lệnh");
        
        if (!confirm) return;

        Order.Status = "Cancelled";
        if (await _productionService.UpdateWorkOrderAsync(Order))
        {
            // Log as "Xóa" as requested by user for this context
            await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, "Xóa", "WorkOrders", null, $"Lệnh {Order.Wocode}");
            _notificationService.ShowSuccess($"Đã hủy lệnh {Order.Wocode}");
            await LoadOrder(Order.Wocode);
        }
        else
        {
            _notificationService.ShowError("Lỗi cập nhật CSDL.");
        }
    }

}
