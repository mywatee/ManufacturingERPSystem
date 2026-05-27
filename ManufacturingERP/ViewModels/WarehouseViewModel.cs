using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;

public partial class WarehouseViewModel : ViewModelBase
{
    private readonly IAccessControlService _accessControlService;
    private readonly INotificationService _notificationService;
    private readonly IWarehouseService _warehouseService;
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private List<InventoryItemDisplay> _allInventories = new();

    [ObservableProperty] private string _title = "Quản lý kho bãi";
    [ObservableProperty] private string _selectedTab = "overview";

    // Summary Stats
    [ObservableProperty] private long _totalItems;
    [ObservableProperty] private decimal _totalInventoryValue;
    [ObservableProperty] private int _lowStockAlerts;
    [ObservableProperty] private int _uniqueMaterialsCount;


    // Transaction Stats
    [ObservableProperty] private double _todayInboundQty;
    [ObservableProperty] private int _todayInboundCount;
    [ObservableProperty] private double _todayOutboundQty;
    [ObservableProperty] private int _todayOutboundCount;
    [ObservableProperty] private double _totalUsedPercentage;

    // Alert Stats
    [ObservableProperty] private int _criticalAlertCount;
    [ObservableProperty] private int _warningAlertCount;
    [ObservableProperty] private int _noticeAlertCount;

    public ObservableCollection<InventoryItemDisplay> Inventory { get; } = new();
    public ObservableCollection<StockTransactionDisplay> Transactions { get; } = new();
    public ObservableCollection<WarehouseConfig> WarehouseConfigs { get; } = new();
    public ObservableCollection<StockAlertDisplay> StockAlerts { get; } = new();
    public ObservableCollection<string> WarehouseList { get; } = new();

    
    [ObservableProperty] private WarehouseConfig? _selectedWarehouseConfig;
    [ObservableProperty] private string _selectedWarehouse = "Tất cả";
    [ObservableProperty] private string _searchQuery = string.Empty;

    // Aggregated Config Stats
    [ObservableProperty] private int _totalWarehousesCount;
    [ObservableProperty] private int _activeWarehousesCount;
    [ObservableProperty] private double _totalCapacitySum;
    [ObservableProperty] private double _totalUsedSum;

    [ObservableProperty] private bool _canAddWarehouse;
    [ObservableProperty] private bool _canEditWarehouse;
    [ObservableProperty] private bool _canDeleteWarehouse;

    public WarehouseViewModel(
        IAccessControlService accessControlService, 
        INotificationService notificationService,
        IWarehouseService warehouseService,
        INavigationService navigationService,
        IAuthService authService)
    {
        _accessControlService = accessControlService;
        _notificationService = notificationService;
        _warehouseService = warehouseService;
        _navigationService = navigationService;
        _authService = authService;
    }

    public override async Task InitializeAsync()
    {
        await LoadPermissionsAsync();
        await LoadWarehouseListAsync();
        await LoadDataAsync();
    }

    private async Task LoadPermissionsAsync()
    {
        await _accessControlService.RefreshAsync();
        var perms = ModulePermissionStateFactory.FromAccessControl(_accessControlService, SystemModules.Warehouse);
        CanAddWarehouse = perms.CanAdd;
        CanEditWarehouse = perms.CanEdit;
        CanDeleteWarehouse = perms.CanDelete;
    }

    // Load warehouse list separately so filtering doesn't cascade ComboBox resets
    private async Task LoadWarehouseListAsync()
    {
        var configs = await _warehouseService.GetWarehousesAsync();
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            WarehouseList.Clear();
            WarehouseList.Add("Tất cả");
            foreach (var c in configs)
            {
                WarehouseList.Add(c.Name);
            }
        });
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var configs = await _warehouseService.GetWarehousesAsync();
            var inventories = await _warehouseService.GetInventoryAsync(SelectedWarehouse);
            var transactions = await _warehouseService.GetTransactionsAsync(SelectedWarehouse);
            var alerts = await _warehouseService.GetStockAlertsAsync();

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                WarehouseConfigs.Clear();
                foreach (var c in configs)
                {
                    WarehouseConfigs.Add(c);
                }
                SelectedWarehouseConfig = WarehouseConfigs.FirstOrDefault();

                _allInventories = inventories;
                ApplyFilters();

                Transactions.Clear();
                foreach (var t in transactions) Transactions.Add(t);

                StockAlerts.Clear();
                foreach (var a in alerts) StockAlerts.Add(a);

                CalculateStats();
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi khi tải dữ liệu kho: {ex.Message}");
        }
    }

    private void ApplyFilters()
    {
        Inventory.Clear();
        var filtered = _allInventories.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLower().Trim();
            filtered = filtered.Where(i => 
                i.MaterialCode.ToLower().Contains(query) || 
                i.MaterialName.ToLower().Contains(query));
        }

        foreach (var i in filtered)
        {
            Inventory.Add(i);
        }

        CalculateStats();
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilters();
    }



    private void CalculateStats()
    {
        TotalItems = (long)Inventory.Sum(i => i.CurrentQty);
        TotalInventoryValue = Inventory.Sum(i => i.TotalValue);
        LowStockAlerts = Inventory.Count(i => i.Status != "An toàn");
        UniqueMaterialsCount = Inventory.Select(i => i.MaterialCode).Distinct().Count();


        // Transaction stats for "today"
        var todayStr = DateTime.Now.ToString("yyyy-MM-dd");
        var todayTrans = Transactions.Where(t => t.TransactionDate.Contains(todayStr)).ToList();
        
        TodayInboundQty = todayTrans.Where(t => t.Type == "Nhập kho").Sum(t => t.Quantity);
        TodayInboundCount = todayTrans.Count(t => t.Type == "Nhập kho");
        
        TodayOutboundQty = todayTrans.Where(t => t.Type == "Xuất kho").Sum(t => t.Quantity);
        TodayOutboundCount = todayTrans.Count(t => t.Type == "Xuất kho");

        // Aggregated Warehouse Configuration Stats
        TotalWarehousesCount = WarehouseConfigs.Count;
        ActiveWarehousesCount = WarehouseConfigs.Count(w => w.Status == "Hoạt động");
        TotalCapacitySum = WarehouseConfigs.Sum(w => w.Capacity);
        TotalUsedSum = WarehouseConfigs.Sum(w => w.Used);
        TotalUsedPercentage = TotalCapacitySum > 0 ? (TotalUsedSum / TotalCapacitySum) * 100 : 0;

        // Alert Aggregation
        CriticalAlertCount = StockAlerts.Count(a => a.AlertLevel == "Nguy cấp");
        WarningAlertCount = StockAlerts.Count(a => a.AlertLevel == "Cảnh báo");
        NoticeAlertCount = StockAlerts.Count(a => a.AlertLevel == "Lưu ý");
    }

    [RelayCommand]
    private void SelectTab(string tabName)
    {
        SelectedTab = tabName;
    }

    [RelayCommand]
    private async Task CreatePurchaseOrder(StockAlertDisplay alert)
    {
        if (alert == null) return;
        
        // Navigate to Inbound screen and pre-fill with the shortage amount
        var vm = _navigationService.NavigateTo<CreateTransactionViewModel>();
        
        // alert.Id is "MaterialId_WarehouseId"
        if (int.TryParse(alert.Id.Split('_')[0], out int materialId))
        {
            await vm.InitializeAsync("Nhập kho", materialId, (decimal)alert.ShortageQuantity);
        }
    }

    [RelayCommand]
    private async Task CreateInbound()
    {
        if (!_accessControlService.HasCached(SystemModules.Warehouse, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Kho bãi.");
            return;
        }

        var vm = _navigationService.NavigateTo<CreateTransactionViewModel>();
        await vm.InitializeAsync("Nhập kho");
    }

    [RelayCommand]
    private async Task CreateOutbound()
    {
        if (!_accessControlService.HasCached(SystemModules.Warehouse, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Kho bãi.");
            return;
        }

        var vm = _navigationService.NavigateTo<CreateTransactionViewModel>();
        await vm.InitializeAsync("Xuất kho");
    }

    [RelayCommand]
    private async Task CreateAdjustment()
    {
        if (!_accessControlService.HasCached(SystemModules.Warehouse, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Điều chỉnh trong phân hệ Kho bãi.");
            return;
        }

        var vm = _navigationService.NavigateTo<CreateTransactionViewModel>();
        await vm.InitializeAsync("Điều chỉnh");
    }

    [RelayCommand]
    private async Task CreateWarehouse()
    {
        if (!_accessControlService.HasCached(SystemModules.Warehouse, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Kho bãi.");
            return;
        }

        var vm = _navigationService.NavigateTo<CreateWarehouseViewModel>();
        await vm.InitializeAsync(null);
    }

    [RelayCommand]
    private async Task EditWarehouse(WarehouseConfig config)
    {
        if (config == null) return;
        
        if (!_accessControlService.HasCached(SystemModules.Warehouse, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Kho bãi.");
            return;
        }

        if (int.TryParse(config.Id, out int id))
        {
            var vm = _navigationService.NavigateTo<CreateWarehouseViewModel>();
            await vm.InitializeAsync(id);
        }
    }

    [RelayCommand]
    private async Task ViewWarehouseDetails(WarehouseConfig config)
    {
        if (config == null) return;
        
        if (int.TryParse(config.Id, out int id))
        {
            var vm = _navigationService.NavigateTo<CreateWarehouseViewModel>();
            await vm.InitializeAsync(id, true); // true for read-only
        }
    }

    [RelayCommand]
    private async Task DeleteWarehouse(WarehouseConfig config)
    {
        if (config == null) return;

        if (!_accessControlService.HasCached(SystemModules.Warehouse, PermissionAction.Delete))
        {
            _notificationService.ShowError("Bạn không có quyền Xóa trong phân hệ Kho bãi.");
            return;
        }

        var result = _notificationService.Confirm(
            $"Bạn có chắc chắn muốn xóa nhà kho '{config.Name}'? Dữ liệu này sẽ bị xóa vĩnh viễn và không thể khôi phục.",
            "Xác nhận xóa nhà kho");


        if (result)
        {

            if (int.TryParse(config.Id, out int id))
            {
                var success = await _warehouseService.DeleteWarehouseAsync(id);
                if (success)
                {
                    _notificationService.ShowSuccess("Đã xóa nhà kho thành công.");
                    await LoadWarehouseListAsync();
                    await LoadDataAsync();
                }
                else
                {
                    _notificationService.ShowError("Không thể xóa nhà kho. Vui lòng kiểm tra lại (Có thể kho vẫn còn hàng tồn).");
                }
            }
        }
    }


    [RelayCommand]
    private async Task ViewTransactionDetails(StockTransactionDisplay transaction)
    {
        if (transaction == null) return;
        
        // Use ReferenceDoc if available and not empty, otherwise use a fallback ID-based string
        string refCode = !string.IsNullOrWhiteSpace(transaction.ReferenceDoc) 
            ? transaction.ReferenceDoc 
            : $"ID:{transaction.Id}";
        
        var vm = _navigationService.NavigateTo<CreateTransactionViewModel>();
        await vm.InitializeAsync(transaction.Type, null, 0, refCode);
    }

    [RelayCommand]
    private async Task EditTransaction(StockTransactionDisplay transaction)
    {
        if (transaction == null) return;
        _notificationService.ShowError("Chức năng sửa phiếu đang được phát triển. Vui lòng sử dụng tính năng Hủy và tạo lại phiếu mới để đảm bảo tính toàn vẹn dữ liệu.");
    }

    [RelayCommand]
    private async Task CancelTransaction(StockTransactionDisplay transaction)
    {
        if (transaction == null) return;

        if (!_accessControlService.HasCached(SystemModules.Warehouse, PermissionAction.Delete))
        {
            _notificationService.ShowError("Bạn không có quyền Hủy giao dịch.");
            return;
        }

        var result = _notificationService.Confirm(
            $"Bạn có chắc chắn muốn HỦY giao dịch '{transaction.MaterialName}' ({transaction.Type}) này không?\nHệ thống sẽ tự động thực hiện hoàn tác tồn kho.",
            "Xác nhận hủy phiếu");

        if (result)
        {
            try
            {
                // To cancel, we create an opposite transaction
                string reverseType = transaction.Type == "Nhập kho" ? "Xuất kho" : "Nhập kho";
                
                // Get IDs from string IDs
                if (int.TryParse(transaction.Id, out int transId))
                {
                    bool ok = await _warehouseService.CancelTransactionAsync(transId, _authService.CurrentUser?.UserId ?? 0);
                    
                    if (ok)
                    {
                        _notificationService.ShowSuccess("Đã hủy phiếu và hoàn tác tồn kho thành công.");
                        await LoadDataAsync();
                    }
                    else
                    {
                        _notificationService.ShowError("Không thể hủy phiếu. Vui lòng kiểm tra lại tồn kho (có thể không đủ hàng để hoàn tác).");
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi hủy phiếu: " + ex.Message);
            }
        }
    }

    [RelayCommand]
    private async Task NavigateToReport()
    {
        var vm = _navigationService.NavigateTo<WarehouseReportViewModel>();
        await vm.InitializeAsync();
    }

    private int _loadVersion;
    private string _prevSelectedWarehouse = "Tất cả";
    partial void OnSelectedWarehouseChanged(string value)
    {
        if (value == _prevSelectedWarehouse) return;
        _prevSelectedWarehouse = value;
        var currentVersion = ++_loadVersion;
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await Task.Delay(100);
            if (currentVersion == _loadVersion)
                await LoadDataAsync();
        });
    }
}

