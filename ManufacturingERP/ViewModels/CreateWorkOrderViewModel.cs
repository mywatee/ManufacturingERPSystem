using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.Core;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;

namespace ManufacturingERP.ViewModels;

public partial class CreateWorkOrderViewModel : ViewModelBase
{
    private readonly IProductionService _productionService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly IAccessControlService _accessControlService;

    [ObservableProperty]
    private string _woCode = string.Empty;
    
    [ObservableProperty]
    private bool _isAutoCode = false;

    [ObservableProperty]
    private ObservableCollection<Material> _products = new();

    [ObservableProperty]
    private ObservableCollection<OrderItemViewModel> _orderItems = new();

    [ObservableProperty]
    private Material? _selectedProduct;

    [ObservableProperty]
    private string _itemQuantity = string.Empty;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Now;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now.AddDays(7);

    [ObservableProperty]
    private bool _isUrgent = false;

    [ObservableProperty]
    private OrderItemViewModel? _selectedOrderItem;

    public bool IsEditing => SelectedOrderItem != null;

    public CreateWorkOrderViewModel(
        IProductionService productionService,
        INavigationService navigationService,
        INotificationService notificationService,
        DashboardViewModel dashboardViewModel,
        IAccessControlService accessControlService)
    {
        _productionService = productionService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _dashboardViewModel = dashboardViewModel;
        _accessControlService = accessControlService;
    }

    public async Task InitializeAsync()
    {
        WoCode = string.Empty;
        IsAutoCode = false;
        var materials = await _productionService.GetProductsAsync();
        Products = new ObservableCollection<Material>(materials);
        
        SelectedProduct = null;
        ItemQuantity = string.Empty;
        OrderItems.Clear();
        StartDate = DateTime.Now;
        EndDate = DateTime.Now.AddDays(7);
        IsUrgent = false;
    }

    partial void OnIsAutoCodeChanged(bool value)
    {
        if (value)
        {
            WoCode = $"LSX-{DateTime.Now:yyyyMMdd-HHmm}";
        }
    }

    partial void OnSelectedOrderItemChanged(OrderItemViewModel? value)
    {
        if (value != null)
        {
            // Sync form with selected item for editing
            var product = Products.FirstOrDefault(p => p.MaterialId == value.ProductId);
            SelectedProduct = product;
            ItemQuantity = value.TargetQty.ToString();
        }
        else
        {
            // Clear form when selection is removed
            SelectedProduct = null;
            ItemQuantity = string.Empty;
        }
        
        OnPropertyChanged(nameof(IsEditing));
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedOrderItem = null;
    }

    [RelayCommand]
    private void AddItem()
    {
        if (SelectedProduct == null)
        {
            _notificationService.ShowWarning("Vui lòng chọn sản phẩm.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ItemQuantity) || !int.TryParse(ItemQuantity, out int qty) || qty <= 0)
        {
            _notificationService.ShowWarning("Vui lòng nhập số lượng hợp lệ.");
            return;
        }

        // Capture product name before it possibly gets cleared by side effects
        string productName = SelectedProduct.MaterialName;

        if (IsEditing && SelectedOrderItem != null)
        {
            // Check if OTHER items in the list already have the new ProductId
            var duplicateItem = OrderItems.FirstOrDefault(i => i != SelectedOrderItem && i.ProductId == SelectedProduct.MaterialId);
            
            if (duplicateItem != null)
            {
                // Merge into the existing item
                duplicateItem.TargetQty += qty;
                OrderItems.Remove(SelectedOrderItem);
                _notificationService.ShowSuccess($"Đã gộp số lượng vào sản phẩm {productName} sẵn có.");
            }
            else
            {
                // Simple update
                SelectedOrderItem.ProductId = SelectedProduct.MaterialId;
                SelectedOrderItem.ProductName = SelectedProduct.MaterialName;
                SelectedOrderItem.ProductCode = SelectedProduct.MaterialCode;
                SelectedOrderItem.TargetQty = qty;
                SelectedOrderItem.Unit = SelectedProduct.Unit ?? "Cái";
                _notificationService.ShowSuccess($"Đã cập nhật sản phẩm {productName}");
            }
            
            SelectedOrderItem = null; // Exit edit mode
        }
        else
        {
            // Check if product already exists in list (only when adding new)
            var existing = OrderItems.FirstOrDefault(i => i.ProductId == SelectedProduct.MaterialId);
            if (existing != null)
            {
                existing.TargetQty += qty;
                _notificationService.ShowSuccess($"Đã cộng thêm {qty} vào sản phẩm {SelectedProduct.MaterialName}");
            }
            else
            {
                OrderItems.Add(new OrderItemViewModel
                {
                    ProductId = SelectedProduct.MaterialId,
                    ProductName = SelectedProduct.MaterialName,
                    ProductCode = SelectedProduct.MaterialCode,
                    TargetQty = qty,
                    Unit = SelectedProduct.Unit ?? "Cái"
                });
                _notificationService.ShowSuccess($"Đã thêm sản phẩm {SelectedProduct.MaterialName}");
            }
        }

        // Reset inputs
        SelectedProduct = null;
        ItemQuantity = string.Empty;
    }

    [RelayCommand]
    private void SetSelectedItem(OrderItemViewModel item)
    {
        SelectedOrderItem = item;
    }

    [RelayCommand]
    private void RemoveItem(OrderItemViewModel item)
    {
        if (item != null)
        {
            OrderItems.Remove(item);
        }
    }

    [RelayCommand]
    private void ClearAllItems()
    {
        if (OrderItems.Count > 0)
        {
            OrderItems.Clear();
            _notificationService.ShowSuccess(" Đã xóa toàn bộ danh mục sản phẩm.");
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!await _accessControlService.HasAsync(SystemModules.Production, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Sản xuất.");
            return;
        }

        if (string.IsNullOrWhiteSpace(WoCode))
        {
            _notificationService.ShowWarning("Vui lòng nhập mã lệnh sản xuất.");
            return;
        }

        if (OrderItems.Count == 0)
        {
            _notificationService.ShowWarning("Lệnh sản xuất phải có ít nhất một sản phẩm.");
            return;
        }

        if (EndDate.Date < DateTime.Today)
        {
            _notificationService.ShowWarning("Hạn hoàn thành không thể ở trong quá khứ.");
            return;
        }

        if (StartDate.Date < DateTime.Today)
        {
            _notificationService.ShowWarning("Ngày bắt đầu không thể ở trong quá khứ.");
            return;
        }

        if (StartDate > EndDate)
        {
            _notificationService.ShowWarning("Ngày bắt đầu phải trước hoặc bằng hạn hoàn thành.");
            return;
        }

        var newOrder = new WorkOrder
        {
            Wocode = WoCode,
            Status = "Planned",
            StartDate = StartDate,
            EndDate = EndDate,
            IsUrgent = IsUrgent,
            WorkOrderItems = OrderItems.Select(i => new WorkOrderItem
            {
                ProductId = i.ProductId,
                TargetQty = i.TargetQty,
                ActualQty = 0,
                Status = "Planned"
            }).ToList()
        };

        var success = await _productionService.CreateWorkOrderAsync(newOrder);
        if (success)
        {
            _notificationService.ShowSuccess($"Đã khởi tạo lệnh sản xuất {WoCode} thành công!");
            await _dashboardViewModel.InitializeAsync();
            await _dashboardViewModel.LogActivityAsync("Thêm", $"Lệnh sản xuất {WoCode} ({OrderItems.Count} sản phẩm)");

            var vm = _navigationService.NavigateTo<ProductionViewModel>();
            await vm.LoadDataAsync();
        }
        else
        {
            _notificationService.ShowError("Có lỗi xảy ra khi lưu lệnh sản xuất.");
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        var vm = _navigationService.NavigateTo<ProductionViewModel>();
        await vm.LoadDataAsync();
    }
}

public partial class OrderItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _productId;

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _productCode = string.Empty;
    
    [ObservableProperty]
    private int _targetQty;
    
    [ObservableProperty]
    private string _unit = "Cái";
}
