using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;

namespace ManufacturingERP.ViewModels;

public partial class CreateTransactionViewModel : ViewModelBase
{
    private readonly IWarehouseService _warehouseService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;

    [ObservableProperty] private string _pageTitle = "Tạo phiếu nhập kho";
    [ObservableProperty] private string _transactionType = "Nhập kho";
    [ObservableProperty] private Warehouse? _selectedWarehouse;
    [ObservableProperty] private Warehouse? _destinationWarehouse;
    [ObservableProperty] private DateTime _transactionDate = DateTime.Now;
    [ObservableProperty] private string _referenceCode = "";
    [ObservableProperty] private Partner? _selectedPartner;
    [ObservableProperty] private string _supplierName = "";
    [ObservableProperty] private string _notes = "";
    [ObservableProperty] private bool _isReadOnly = false;
    [ObservableProperty] private bool _autoCreateInvoice = true;
    
    public bool IsTransferType => TransactionType == "Chuyển kho";
    public bool ShowPartner => TransactionType == "Nhập kho" || TransactionType == "Xuất kho";
    public bool IsEditable => !IsReadOnly;

    partial void OnTransactionTypeChanged(string value)
    {
        if (IsReadOnly)
        {
            PageTitle = $"Chi tiết {value.ToLower()}";
            return;
        }

        PageTitle = value switch
        {
            "Nhập kho" => "Phiếu nhập kho mới",
            "Xuất kho" => "Phiếu xuất kho mới",
            "Chuyển kho" => "Phiếu chuyển kho nội bộ",
            "Điều chỉnh" => "Phiếu kiểm kê & Điều chỉnh",
            _ => "Giao dịch kho mới"
        };
        OnPropertyChanged(nameof(IsTransferType));
        OnPropertyChanged(nameof(ShowPartner));
        OnPropertyChanged(nameof(IsEditable));
        _ = UpdateNewItemStockAsync();
    }

    // Search logic for Partners
    private string _partnerSearchText = "";
    public string PartnerSearchText
    {
        get => _partnerSearchText;
        set
        {
            if (SetProperty(ref _partnerSearchText, value))
            {
                FilterPartners();
            }
        }
    }

    private bool _isUpdatingFromSelection = false;
    private void FilterPartners()
    {
        if (_isUpdatingFromSelection) return;

        var searchText = PartnerSearchText ?? "";
        
        App.Current.Dispatcher.Invoke(() => {
            FilteredPartners.Clear();
            var matches = string.IsNullOrWhiteSpace(searchText) 
                ? Partners.ToList()
                : Partners.Where(p => p.PartnerName.ToLower().Contains(searchText.ToLower())).ToList();

            foreach (var m in matches) FilteredPartners.Add(m);
        });
    }

    // Material Search Logic
    private string _materialSearchText = "";
    public string MaterialSearchText
    {
        get => _materialSearchText;
        set
        {
            if (SetProperty(ref _materialSearchText, value))
            {
                FilterMaterials();
            }
        }
    }

    private void FilterMaterials()
    {
        if (_isUpdatingFromSelection) return;

        var searchText = MaterialSearchText ?? "";
        
        App.Current.Dispatcher.Invoke(() => {
            FilteredMaterials.Clear();
            var matches = string.IsNullOrWhiteSpace(searchText) 
                ? Materials.ToList()
                : Materials.Where(m => m.MaterialName.ToLower().Contains(searchText.ToLower()) || 
                                      (m.MaterialCode != null && m.MaterialCode.ToLower().Contains(searchText.ToLower()))).ToList();

            foreach (var m in matches) FilteredMaterials.Add(m);
        });
    }



    [ObservableProperty] private Material? _newItemMaterial;
    [ObservableProperty] private string _newItemQuantity = "0";
    [ObservableProperty] private string _newItemUnit = "-";
    [ObservableProperty] private double _newItemCurrentStock = 0;
    [ObservableProperty] private double _newItemDestStock = 0;

    public ObservableCollection<Partner> Partners { get; } = new();
    public ObservableCollection<Partner> FilteredPartners { get; } = new();
    public ObservableCollection<Warehouse> Warehouses { get; } = new();
    public ObservableCollection<Material> Materials { get; } = new();
    public ObservableCollection<Material> FilteredMaterials { get; } = new();

    public ObservableCollection<string> TransactionTypes { get; } = new() { "Nhập kho", "Xuất kho", "Chuyển kho", "Điều chỉnh" };
    public ObservableCollection<WarehouseTransactionItem> TransactionItems { get; } = new();

    partial void OnSelectedPartnerChanged(Partner? value)
    {
        if (value != null)
        {
            _isUpdatingFromSelection = true;
            PartnerSearchText = value.PartnerName;
            SupplierName = value.PartnerName;
            _isUpdatingFromSelection = false;
        }
    }


    public CreateTransactionViewModel(
        IWarehouseService warehouseService,
        INavigationService navigationService,
        INotificationService notificationService,
        IAuthService authService,
        IAuditLogService auditLogService,
        IDbContextFactory<ManufacturingContext> contextFactory)
    {
        _warehouseService = warehouseService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _authService = authService;
        _auditLogService = auditLogService;
        _contextFactory = contextFactory;
    }

    public override async Task InitializeAsync() => await InitializeAsync("Nhập kho");

    public async Task InitializeAsync(string type, int? prefillMaterialId = null, decimal prefillQty = 0, string? viewRefCode = null)
    {
        // Important: Set IsReadOnly first
        IsReadOnly = !string.IsNullOrWhiteSpace(viewRefCode);
        TransactionType = type;
        
        // Force title update in case OnTransactionTypeChanged didn't trigger
        if (IsReadOnly) PageTitle = $"Chi tiết {type.ToLower()}";

        await LoadDataAsync();

        if (IsReadOnly && viewRefCode != null)
        {
            PageTitle = viewRefCode.StartsWith("ID:") ? "Chi tiết giao dịch" : $"Chi tiết chứng từ: {viewRefCode}";
            var trans = await _warehouseService.GetTransactionsByReferenceAsync(viewRefCode);
            
            if (trans.Any())
            {
                var first = trans.First();
                TransactionDate = first.TransDate ?? DateTime.Now;
                ReferenceCode = first.ReferenceCode ?? "";
                Notes = first.Notes ?? "";
                SelectedWarehouse = Warehouses.FirstOrDefault(w => w.WarehouseId == first.WarehouseId);
                SelectedPartner = Partners.FirstOrDefault(p => p.PartnerId == first.PartnerId);
                
                App.Current.Dispatcher.Invoke(() => {
                    TransactionItems.Clear();
                    foreach (var t in trans)
                    {
                        TransactionItems.Add(new WarehouseTransactionItem
                        {
                            MaterialId = t.MaterialId ?? 0,
                            MaterialCode = t.Material?.MaterialCode ?? "",
                            MaterialName = t.Material?.MaterialName ?? "",
                            Quantity = t.Quantity ?? 0,
                            Unit = t.Material?.Unit ?? "-"
                        });
                    }
                });
            }
            else
            {
                _notificationService.ShowError("Không tìm thấy dữ liệu chi tiết cho chứng từ này.");
                IsReadOnly = false; // Fallback to normal mode if not found? Or just return?
                await Cancel(); // Go back
            }
        }
        else
        {
            PageTitle = type switch
            {
                "Nhập kho" => "Phiếu nhập kho mới",
                "Xuất kho" => "Phiếu xuất kho mới",
                "Chuyển kho" => "Phiếu chuyển kho nội bộ",
                "Điều chỉnh" => "Phiếu kiểm kê & Điều chỉnh",
                _ => "Giao dịch kho mới"
            };
            
            // Handle pre-fill if material and qty are provided
            if (prefillMaterialId.HasValue)
            {
                var material = Materials.FirstOrDefault(m => m.MaterialId == prefillMaterialId.Value);
                if (material != null)
                {
                    NewItemMaterial = material;
                    NewItemQuantity = prefillQty > 0 ? prefillQty.ToString() : "0";
                    
                    if (prefillQty > 0)
                    {
                        AddItem();
                    }
                }
            }
        }
        
        OnPropertyChanged(nameof(IsEditable));
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load Warehouses, Materials, and Partners
            using var context = await _contextFactory.CreateDbContextAsync();
            var warehouses = await context.Warehouses.ToListAsync();
            var materials = await context.Materials
                .Where(m => m.Status == "Đang sử dụng")
                .OrderBy(m => m.MaterialName)
                .ToListAsync();
            var partners = await context.Partners.OrderBy(p => p.PartnerName).ToListAsync();


            App.Current.Dispatcher.Invoke(() =>
            {
                Warehouses.Clear();
                foreach (var w in warehouses) Warehouses.Add(w);
                
                Materials.Clear();
                FilteredMaterials.Clear();
                foreach (var m in materials) 
                {
                    Materials.Add(m);
                    FilteredMaterials.Add(m);
                }


                Partners.Clear();
                FilteredPartners.Clear();
                foreach (var p in partners) 
                {
                    if (Partners.All(existing => existing.PartnerId != p.PartnerId))
                    {
                        Partners.Add(p);
                        FilteredPartners.Add(p);
                    }
                }


                if (Warehouses.Any() && !IsReadOnly) SelectedWarehouse = Warehouses[0];
            });

        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi tải dữ liệu: " + ex.Message);
        }
    }

    partial void OnNewItemMaterialChanged(Material? value)
    {
        if (value != null)
        {
            _isUpdatingFromSelection = true;
            MaterialSearchText = value.MaterialName;
            _isUpdatingFromSelection = false;
        }
        
        NewItemUnit = value?.Unit ?? "-";
        _ = UpdateNewItemStockAsync();
    }



    partial void OnDestinationWarehouseChanged(Warehouse? value)
    {
        _ = UpdateNewItemStockAsync();
    }

    private async Task UpdateNewItemStockAsync()
    {
        if (NewItemMaterial == null)
        {
            NewItemCurrentStock = 0;
            NewItemDestStock = 0;
            return;
        }

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Check Source/Main Warehouse
            if (SelectedWarehouse != null)
            {
                var inv = await context.Inventories
                    .FirstOrDefaultAsync(i => i.MaterialId == NewItemMaterial.MaterialId && i.WarehouseId == SelectedWarehouse.WarehouseId);
                NewItemCurrentStock = (double)(inv?.CurrentStock ?? 0);
            }
            else NewItemCurrentStock = 0;

            // Check Destination Warehouse
            if (DestinationWarehouse != null)
            {
                var invDest = await context.Inventories
                    .FirstOrDefaultAsync(i => i.MaterialId == NewItemMaterial.MaterialId && i.WarehouseId == DestinationWarehouse.WarehouseId);
                NewItemDestStock = (double)(invDest?.CurrentStock ?? 0);
            }
            else NewItemDestStock = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetStockInfo error: {ex.Message}");
            NewItemCurrentStock = 0;
            NewItemDestStock = 0;
        }
    }


    [RelayCommand]
    private void AddItem()
    {
        if (NewItemMaterial == null)
        {
            _notificationService.ShowError("Vui lòng chọn vật tư.");
            return;
        }

        if (!decimal.TryParse(NewItemQuantity, out decimal qty) || qty <= 0)
        {
            _notificationService.ShowError("Số lượng phải lớn hơn 0.");
            return;
        }

        if (IsTransferType)
        {
            if (SelectedWarehouse?.WarehouseId == DestinationWarehouse?.WarehouseId)
            {
                _notificationService.ShowError("Kho nguồn và kho đích không được trùng nhau.");
                return;
            }
            if (DestinationWarehouse == null)
            {
                _notificationService.ShowError("Vui lòng chọn kho đích.");
                return;
            }
        }

        if ((TransactionType == "Xuất kho" || TransactionType == "Chuyển kho") && (decimal)NewItemCurrentStock < qty)
        {
            _notificationService.ShowError("Số lượng chuyển/xuất vượt quá tồn kho hiện tại.");
            return;
        }
        
        // Label for Adjustment in the grid
        string displayQty = TransactionType == "Điều chỉnh" ? $"Set to {qty}" : qty.ToString();

        // Check if material already exists in list
        var existing = TransactionItems.FirstOrDefault(i => i.MaterialId == NewItemMaterial.MaterialId);
        if (existing != null)
        {
            existing.Quantity += qty;
        }
        else
        {
            TransactionItems.Add(new WarehouseTransactionItem
            {
                MaterialId = NewItemMaterial.MaterialId,
                MaterialCode = NewItemMaterial.MaterialCode,
                MaterialName = NewItemMaterial.MaterialName,
                Quantity = qty,
                Unit = NewItemMaterial.Unit ?? "-"
            });
        }

        // Reset inputs
        NewItemMaterial = null;
        NewItemQuantity = "0";
    }

    [RelayCommand]
    private void RemoveItem(WarehouseTransactionItem item)
    {
        if (item != null)
        {
            TransactionItems.Remove(item);
        }
    }

    [RelayCommand]
    private void EditItem(WarehouseTransactionItem item)
    {
        if (item == null) return;
        
        var material = Materials.FirstOrDefault(m => m.MaterialId == item.MaterialId);
        if (material != null)
        {
            _isUpdatingFromSelection = true;
            NewItemMaterial = material;
            MaterialSearchText = material.MaterialName;
            NewItemQuantity = item.Quantity.ToString();
            _isUpdatingFromSelection = false;
            
            // Remove the item from list so when user clicks "Thêm" it updates/re-adds it
            TransactionItems.Remove(item);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (SelectedWarehouse == null)
        {
            _notificationService.ShowError("Vui lòng chọn nhà kho.");
            return;
        }

        if (IsTransferType && DestinationWarehouse == null)
        {
            _notificationService.ShowError("Vui lòng chọn kho đích.");
            return;
        }

        if (TransactionItems.Count == 0)
        {
            _notificationService.ShowError("Vui lòng thêm ít nhất một vật tư vào phiếu.");
            return;
        }

        if (IsTransferType || TransactionType == "Điều chỉnh")
        {
            bool allSuccess = true;
            foreach (var item in TransactionItems)
            {
                bool success;
                if (IsTransferType)
                {
                    success = await _warehouseService.TransferStockAsync(
                        SelectedWarehouse.WarehouseId,
                        DestinationWarehouse!.WarehouseId,
                        item.MaterialId,
                        item.Quantity,
                        _authService.CurrentUser?.UserId);
                }
                else // Adjustment
                {
                    success = await _warehouseService.AdjustStockAsync(
                        SelectedWarehouse.WarehouseId,
                        item.MaterialId,
                        item.Quantity,
                        _authService.CurrentUser?.UserId,
                        Notes);
                }
                if (!success) allSuccess = false;
            }
            
            if (allSuccess)
            {
                await FinalizeSave();
            }
            else
            {
                _notificationService.ShowError("Có lỗi xảy ra khi thực hiện giao dịch. Vui lòng kiểm tra lại tồn kho.");
            }
        }
        else // Nhập kho / Xuất kho
        {
            var transactions = TransactionItems.Select(item => new StockTransaction
            {
                Type = TransactionType,
                MaterialId = item.MaterialId,
                WarehouseId = SelectedWarehouse.WarehouseId,
                Quantity = item.Quantity,
                ReferenceCode = ReferenceCode?.Trim(),
                TransBy = _authService.CurrentUser?.UserId,
                TransDate = TransactionDate,
                PartnerId = SelectedPartner?.PartnerId,
                Notes = Notes
            }).ToList();

            var success = await _warehouseService.AddStockTransactionsAsync(transactions);
            if (success)
            {
                // Auto create invoice if requested
                if (AutoCreateInvoice && SelectedPartner != null && (TransactionType == "Nhập kho" || TransactionType == "Xuất kho"))
                {
                    await CreateAutomaticInvoiceAsync(transactions);
                }
                
                await FinalizeSave();
            }
            else
            {
                _notificationService.ShowError("Có lỗi xảy ra khi thực hiện giao dịch. Có thể do thiếu hàng tồn kho.");
            }
        }
    }

    private async Task CreateAutomaticInvoiceAsync(List<StockTransaction> transactions)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            // Re-fetch materials to get UnitPrice
            var materialIds = transactions.Select(t => t.MaterialId).ToList();
            var materials = await context.Materials
                .Where(m => materialIds.Contains(m.MaterialId))
                .ToDictionaryAsync(m => m.MaterialId, m => m);

            var invoice = new Invoice
            {
                InvoiceCode = "INV-AUTO-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                PartnerId = SelectedPartner!.PartnerId,
                Type = TransactionType == "Nhập kho" ? "AP" : "AR",
                IssueDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(30),
                Reference = ReferenceCode,
                Note = "Hóa đơn được tạo tự động từ phiếu " + TransactionType.ToLower(),
                Status = "Chưa thanh toán"
            };

            decimal totalAmount = 0;
            foreach (var t in transactions)
            {
                if (materials.TryGetValue(t.MaterialId ?? 0, out var mat))
                {
                    var unitPrice = mat.UnitPrice ?? 0;
                    var qty = (decimal)(t.Quantity ?? 0);
                    var itemTotal = qty * unitPrice;
                    
                    invoice.Items.Add(new InvoiceItem
                    {
                        ProductName = mat.MaterialName,
                        Quantity = qty,
                        UnitPrice = unitPrice
                    });
                    
                    totalAmount += itemTotal;
                }
            }

            invoice.TotalAmount = totalAmount;
            invoice.PaidAmount = 0;
            invoice.VatRate = 0; // Default 0%
            invoice.VatAmount = 0;

            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            
            // Notify other view models
            WeakReferenceMessenger.Default.Send(new InvoiceCreatedMessage(invoice.InvoiceCode));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error creating auto invoice: " + ex.Message);
            _notificationService.ShowError("Kho đã cập nhật nhưng không thể tạo hóa đơn tự động: " + ex.Message);
        }
    }

    private async Task FinalizeSave()
    {
        string logMsg = IsTransferType 
            ? $"Chuyển {TransactionItems.Count} mặt hàng từ {SelectedWarehouse?.WarehouseName} sang {DestinationWarehouse?.WarehouseName}"
            : $"{TransactionType} {TransactionItems.Count} mặt hàng. Tham chiếu: {ReferenceCode}";

        await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, TransactionType, "StockTransactions", null, logMsg);
        _notificationService.ShowSuccess($"Đã thực hiện {TransactionType.ToLower()} thành công.");
        
        var vm = _navigationService.NavigateTo<WarehouseViewModel>();
        await vm.InitializeAsync();
    }

    [RelayCommand]
    private async Task Cancel()
    {
        var vm = _navigationService.NavigateTo<WarehouseViewModel>();
        await vm.InitializeAsync();
    }
}

public partial class WarehouseTransactionItem : ObservableObject
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    [ObservableProperty] private decimal _quantity;
    public string Unit { get; set; } = "";
}
