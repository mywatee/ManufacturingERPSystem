using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Services;
using ManufacturingERP.Models;
using ManufacturingERP.Views.Dialogs;

namespace ManufacturingERP.ViewModels;

public partial class MasterDataViewModel : ViewModelBase
{
    private readonly IAccessControlService _accessControlService;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IPartnerService _partnerService;
    private readonly IMasterDataService _masterDataService;
    private readonly INavigationService _navigationService;
    private readonly IWarehouseService _warehouseService;

    public ICollectionView MaterialsView { get; }
    public ObservableCollection<MaterialItem> Materials { get; } = new();
    public ObservableCollection<AuditLog> AuditLogs { get; } = new();
    public ObservableCollection<BomLineItem> BomLines { get; } = new();
    public ObservableCollection<RoutingStepItem> RoutingSteps { get; } = new();
    public ObservableCollection<InventoryHistoryItem> MaterialHistory { get; } = new();
    public ObservableCollection<WarehouseConfig> Warehouses { get; } = new();
    public ObservableCollection<PartnerItem> Partners { get; } = new();
    public ObservableCollection<PartnerItem> VisiblePartners { get; } = new();

    [ObservableProperty] private int _partnerCurrentPage = 1;
    [ObservableProperty] private int _partnerTotalPages = 1;
    [ObservableProperty] private int _partnerTotalItems = 0;
    [ObservableProperty] private string _partnerSearchText = string.Empty;
    private const int PartnerPageSize = 5;

    public ObservableCollection<string> CategoryFilters { get; } = new();
    public ObservableCollection<string> StatusFilters { get; } = new();

    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _pageStatusText = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedCategoryFilter = "Tất cả phân loại";
    [ObservableProperty] private string _selectedStatusFilter = "Tất cả trạng thái";
    [ObservableProperty] private double _totalRoutingTime;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddBomLineCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReleaseBomCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveRoutingCommand))]
    [NotifyCanExecuteChangedFor(nameof(ManageRoutingCommand))]
    private MaterialItem? _selectedMaterial;

    partial void OnSelectedMaterialChanged(MaterialItem? value)
    {
        if (value != null)
        {
            _ = LoadBomAndRouting(value);
            LoadMaterialHistory(value);
            _ = LoadAuditLogsAsync(value.Id);
        }
    }

    [RelayCommand]
    private async Task LoadAuditLogs(string? materialCode)
    {
        await LoadAuditLogsAsync(materialCode ?? SelectedMaterial?.Id);
    }

    private async Task LoadAuditLogsAsync(string? materialCode)
    {
        if (string.IsNullOrEmpty(materialCode))
        {
            AuditLogs.Clear();
            AuditLogTotalItems = 0;
            AuditLogTotalPages = 1;
            return;
        }
        try
        {
            var logs = await _auditLogService.GetPagedAsync(AuditLogCurrentPage, AuditLogPageSize, materialCode, AuditLogStartDate, AuditLogEndDate);
            AuditLogs.Clear();
            foreach (var log in logs.Items)
            {
                AuditLogs.Add(log);
            }
            AuditLogTotalItems = logs.TotalCount;
            AuditLogTotalPages = (int)Math.Ceiling((double)logs.TotalCount / AuditLogPageSize);
        }
        catch (Exception) { /* Silent fail for logs */ }
    }

    [RelayCommand(CanExecute = nameof(CanNextAuditPage))]
    private void AuditLogNextPage()
    {
        AuditLogCurrentPage++;
        _ = LoadAuditLogsAsync(SelectedMaterial?.Id);
    }

    private bool CanNextAuditPage() => AuditLogCurrentPage < AuditLogTotalPages;

    [RelayCommand(CanExecute = nameof(CanPreviousAuditPage))]
    private void AuditLogPreviousPage()
    {
        AuditLogCurrentPage--;
        _ = LoadAuditLogsAsync(SelectedMaterial?.Id);
    }

    private bool CanPreviousAuditPage() => AuditLogCurrentPage > 1;

    [RelayCommand]
    private void PartnerNextPage()
    {
        if (PartnerCurrentPage < PartnerTotalPages)
        {
            PartnerCurrentPage++;
            UpdatePartnerPagination();
        }
    }

    [RelayCommand]
    private void PartnerPreviousPage()
    {
        if (PartnerCurrentPage > 1)
        {
            PartnerCurrentPage--;
            UpdatePartnerPagination();
        }
    }

    private void UpdatePartnerPagination()
    {
        var filtered = Partners.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(PartnerSearchText))
        {
            var query = PartnerSearchText.Trim();
            filtered = filtered.Where(p => 
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                p.Code.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = filtered.ToList();
        PartnerTotalItems = filteredList.Count;
        PartnerTotalPages = (int)Math.Ceiling((double)PartnerTotalItems / PartnerPageSize);
        if (PartnerCurrentPage > PartnerTotalPages && PartnerTotalPages > 0) PartnerCurrentPage = PartnerTotalPages;
        if (PartnerTotalPages == 0) PartnerCurrentPage = 1;

        var paged = filteredList.Skip((PartnerCurrentPage - 1) * PartnerPageSize).Take(PartnerPageSize).ToList();
        VisiblePartners.Clear();
        foreach (var p in paged) VisiblePartners.Add(p);
    }

    partial void OnPartnerSearchTextChanged(string value) { PartnerCurrentPage = 1; UpdatePartnerPagination(); }

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(AuditLogNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(AuditLogPreviousPageCommand))]
    private int _auditLogCurrentPage = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AuditLogNextPageCommand))]
    private int _auditLogTotalPages = 1;
    [ObservableProperty] private int _auditLogTotalItems = 0;
    private const int AuditLogPageSize = 10;
    [ObservableProperty] private DateTime _auditLogStartDate = DateTime.Now.AddYears(-1);
    [ObservableProperty] private DateTime _auditLogEndDate = DateTime.Now.AddDays(1);

    partial void OnAuditLogStartDateChanged(DateTime value) { AuditLogCurrentPage = 1; _ = LoadAuditLogsAsync(SelectedMaterial?.Id); }
    partial void OnAuditLogEndDateChanged(DateTime value) { AuditLogCurrentPage = 1; _ = LoadAuditLogsAsync(SelectedMaterial?.Id); }

    [ObservableProperty] private bool _isBomVisible = true;
    [ObservableProperty] private bool _isRoutingVisible = false;
    [ObservableProperty] private bool _isAuditLogVisible = false;
    [ObservableProperty] private bool _isPartnersVisible = false;

    [RelayCommand]
    private void SwitchDetailTab(string tab)
    {
        IsBomVisible = tab == "BOM";
        IsRoutingVisible = tab == "Routing";
        IsAuditLogVisible = tab == "Audit";
        IsPartnersVisible = tab == "Partners";

        if (IsPartnersVisible)
        {
            _ = LoadPartnersAsync();
        }
    }

    [RelayCommand]
    private void NavigateToImport()
    {
        _navigationService.NavigateTo<MasterDataImportExportViewModel>();
    }

    public MasterDataViewModel(IAccessControlService accessControlService, INotificationService notificationService, INavigationService navigationService, IMasterDataService masterDataService, IWarehouseService warehouseService, IAuditLogService auditLogService, IPartnerService partnerService)
    {
        _accessControlService = accessControlService;
        _notificationService = notificationService;
        _navigationService = navigationService;
        _masterDataService = masterDataService;
        _warehouseService = warehouseService;
        _auditLogService = auditLogService;
        _partnerService = partnerService;

        RoutingSteps.CollectionChanged += (s, e) => RecalculateTotalRoutingTime();

        MaterialsView = CollectionViewSource.GetDefaultView(Materials);
        MaterialsView.Filter = MaterialsFilter;

        CategoryFilters.Add("Tất cả phân loại");
        CategoryFilters.Add("Nguyên liệu");
        CategoryFilters.Add("Bán thành phẩm");
        CategoryFilters.Add("Thành phẩm");

        StatusFilters.Add("Tất cả trạng thái");
        StatusFilters.Add("Đang sử dụng");
        StatusFilters.Add("Sắp hết");
        StatusFilters.Add("Hết hàng");
        StatusFilters.Add("Ngừng sử dụng");
        
        _ = LoadMaterialsAsync();
        _ = LoadWarehousesAsync();
        _ = LoadPartnersAsync();
        
        UpdatePagination();
    }

    public async Task LoadPartnersAsync()
    {
        try
        {
            var dbPartners = await _partnerService.GetAllAsync();
            Partners.Clear();
            foreach (var p in dbPartners)
            {
                Partners.Add(new PartnerItem
                {
                    Id = p.PartnerId.ToString(),
                    Code = p.PartnerCode,
                    Name = p.PartnerName,
                    Type = p.PartnerType ?? "N/A",
                    Phone = p.Phone ?? "N/A",
                    Email = p.Email ?? "N/A"
                });
            }
            UpdatePartnerPagination();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Không thể tải danh sách đối tác: " + ex.Message);
        }
    }

    public async Task LoadWarehousesAsync()
    {
        try
        {
            var dbWarehouses = await _warehouseService.GetWarehousesAsync();
            Warehouses.Clear();
            foreach (var w in dbWarehouses)
            {
                Warehouses.Add(w);
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Không thể tải danh sách kho: " + ex.Message);
        }
    }

    public async Task LoadMaterialsAsync()
    {
        try 
        {
            var dbMaterials = await _masterDataService.GetAllMaterialsAsync();
            Materials.Clear();
            foreach (var m in dbMaterials)
            {
                Materials.Add(new MaterialItem
                {
                    DbId = m.MaterialId,
                    Id = m.MaterialCode,
                    Name = m.MaterialName,
                    Unit = m.Unit ?? "Cái",
                    Category = m.Category ?? "Nguyên liệu",
                    Status = m.Status ?? "Đang sử dụng",
                    OnHand = (double)(m.Inventories?.Sum(i => i.CurrentStock ?? 0) ?? 0),
                    MinStock = m.MinStock ?? 10,
                    UnitPrice = m.UnitPrice ?? 0
                });
            }
            UpdatePagination();
            if (SelectedMaterial == null && Materials.Count > 0)
            {
                SelectedMaterial = Materials[0];
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Không thể tải danh sách vật tư: " + ex.Message);
        }
    }

    private void UpdatePagination()
    {
        var filteredList = Materials.Where(m => MaterialsFilter(m)).ToList();
        TotalItems = filteredList.Count;
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
        if (TotalPages == 0) CurrentPage = 1;
        
        PageStatusText = $"Hiển thị {Math.Min((CurrentPage - 1) * PageSize + 1, TotalItems)} - {Math.Min(CurrentPage * PageSize, TotalItems)} trong tổng số {TotalItems}";
        MaterialsView.Refresh();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            UpdatePagination();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdatePagination();
        }
    }

    partial void OnSearchTextChanged(string value) { CurrentPage = 1; UpdatePagination(); }
    partial void OnSelectedCategoryFilterChanged(string value) { CurrentPage = 1; UpdatePagination(); }
    partial void OnSelectedStatusFilterChanged(string value) { CurrentPage = 1; UpdatePagination(); }

    private bool MaterialsFilter(object obj)
    {
        if (obj is not MaterialItem material) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            if (!material.Id.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                !material.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!string.Equals(SelectedCategoryFilter, "Tất cả phân loại", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(material.Category, SelectedCategoryFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(SelectedStatusFilter, "Tất cả trạng thái", StringComparison.OrdinalIgnoreCase))
        {
            if (SelectedStatusFilter == "Sắp hết")
            {
                if (!material.IsLowStock) return false;
            }
            else if (SelectedStatusFilter == "Hết hàng")
            {
                if (!material.IsOutOfStock) return false;
            }
            else if (!string.Equals(material.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    [RelayCommand]
    private void AddMaterial()
    {
        if (!_accessControlService.HasCached(SystemModules.MasterData, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm vật tư.");
            return;
        }
        _navigationService.NavigateTo<CreateMaterialViewModel>();
    }

    [RelayCommand]
    private async Task EditMaterial(MaterialItem? material)
    {
        if (material == null) return;
        if (!_accessControlService.HasCached(SystemModules.MasterData, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Chỉnh sửa vật tư.");
            return;
        }
        var createVM = _navigationService.NavigateTo<CreateMaterialViewModel>();
        await createVM.LoadMaterialAsync(material.Id);
    }

    [RelayCommand]
    private void ViewMaterialDetails(MaterialItem? material)
    {
        if (material == null) return;
        SelectedMaterial = material;
    }

    [RelayCommand]
    private async Task DeleteMaterial(MaterialItem? material)
    {
        if (material == null) return;
        if (!_accessControlService.HasCached(SystemModules.MasterData, PermissionAction.Delete))
        {
            _notificationService.ShowError("Bạn không có quyền Xóa vật tư.");
            return;
        }

        if (_notificationService.Confirm($"Bạn có chắc chắn muốn xóa vật tư {material.Id} - {material.Name}?\nHành động này không thể hoàn tác.", "Xác nhận xóa"))
        {
            var result = await _masterDataService.DeleteMaterialDetailedAsync(material.DbId);
            if (result.Success)
            {
                Materials.Remove(material);
                _notificationService.ShowSuccess(result.Message);
            }
            else
            {
                _notificationService.ShowError(result.Message);
            }
        }
    }

    [ObservableProperty] private bool _isRoutingDirty;

    [RelayCommand(CanExecute = nameof(CanEditSelectedMaterial))]
    private void ManageRouting()
    {
        if (!_accessControlService.HasCached(SystemModules.MasterData, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa quy trình.");
            return;
        }

        var currentEntries = RoutingSteps.Select(s => new RoutingStepDialog.RoutingStepEntry
        {
            StepNo = s.StepNo,
            StepName = s.StepName,
            WorkCenter = s.WorkCenter,
            StdTimeMinutes = s.StdTimeMinutes,
            Output = s.Output
        }).ToList();

        var nextStepNo = RoutingSteps.Count == 0 ? 10 : (RoutingSteps.Max(s => s.StepNo) + 10);
        var dialog = new RoutingStepDialog(currentEntries, nextStepNo)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.Steps.Any())
        {
            RoutingSteps.Clear();
            foreach (var entry in dialog.Steps.Where(s => !string.IsNullOrWhiteSpace(s.StepName)))
            {
                RoutingSteps.Add(new RoutingStepItem
                {
                    StepNo = entry.StepNo,
                    StepName = entry.StepName,
                    WorkCenter = entry.WorkCenter,
                    StdTimeMinutes = entry.StdTimeMinutes,
                    Output = entry.Output
                });
            }
            IsRoutingDirty = true;
        }
    }

    [RelayCommand]
    private void DeleteRoutingStep(RoutingStepItem? step)
    {
        if (step == null) return;
        if (_notificationService.Confirm($"Xác nhận xóa công đoạn {step.StepNo} - {step.StepName}?", "Xác nhận xóa"))
        {
            RoutingSteps.Remove(step);
            IsRoutingDirty = true;
        }
    }

    [RelayCommand]
    private void AddWarehouse()
    {
        var dialog = new WarehouseConfigDialog() { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() == true && dialog.NewWarehouse != null)
        {
            _ = SaveNewWarehouse(dialog.NewWarehouse);
        }
    }

    private async Task SaveNewWarehouse(Warehouse dbWarehouse)
    {
        var success = await _warehouseService.AddWarehouseAsync(dbWarehouse);
        if (success)
        {
            Warehouses.Add(new WarehouseConfig { Id = dbWarehouse.WarehouseId.ToString(), Code = dbWarehouse.Code, Name = dbWarehouse.WarehouseName, Location = dbWarehouse.Location, Capacity = (int)dbWarehouse.Capacity, Status = dbWarehouse.Status });
            _notificationService.ShowSuccess($"Đã lưu nhà kho mới: {dbWarehouse.WarehouseName}");
        }
    }

    [RelayCommand]
    private void AddPartner()
    {
        var dialog = new Views.Dialogs.PartnerDialog() { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() == true && dialog.Partner != null)
        {
            _ = SaveNewPartner(dialog.Partner);
        }
    }

    private async Task SaveNewPartner(Partner partner)
    {
        var success = await _partnerService.AddAsync(partner);
        if (success)
        {
            Partners.Add(new PartnerItem { Id = partner.PartnerId.ToString(), Code = partner.PartnerCode, Name = partner.PartnerName, Type = partner.PartnerType ?? "N/A", Phone = partner.Phone ?? "N/A", Email = partner.Email ?? "N/A" });
            UpdatePartnerPagination();
            _notificationService.ShowSuccess($"Đã thêm đối tác: {partner.PartnerName}");
        }
        else
        {
            _notificationService.ShowError("Không thể thêm đối tác. Có thể mã đối tác đã tồn tại hoặc dữ liệu không hợp lệ.");
        }
    }

    [RelayCommand]
    private void EditPartner(PartnerItem item) { if (item == null) return; _ = OpenEditPartnerDialog(item); }

    private async Task OpenEditPartnerDialog(PartnerItem item)
    {
        if (!int.TryParse(item.Id, out int id)) return;
        var partner = await _partnerService.GetByIdAsync(id);
        if (partner == null) return;
        var dialog = new Views.Dialogs.PartnerDialog(partner) { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            var success = await _partnerService.UpdateAsync(partner);
            if (success)
            {
                item.Code = partner.PartnerCode; item.Name = partner.PartnerName; item.Type = partner.PartnerType ?? "N/A"; item.Phone = partner.Phone ?? "N/A"; item.Email = partner.Email ?? "N/A";
                _notificationService.ShowSuccess($"Đã cập nhật đối tác: {partner.PartnerName}");
            }
            else
            {
                _notificationService.ShowError("Không thể cập nhật đối tác. Có thể mã đối tác đã trùng với đối tác khác.");
            }
        }
    }

    [RelayCommand]
    private async Task DeletePartner(PartnerItem partner)
    {
        if (partner == null) return;
        if (_notificationService.Confirm($"Bạn có chắc chắn muốn xóa đối tác {partner.Name}?", "Xác nhận xóa"))
        {
            if (int.TryParse(partner.Id, out int id))
            {
                var success = await _partnerService.DeleteAsync(id);
                if (success) { 
                    Partners.Remove(partner); 
                    UpdatePartnerPagination();
                    _notificationService.ShowSuccess($"Đã xóa đối tác: {partner.Name}"); 
                }
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedMaterial))]
    private void ReleaseBom() { _notificationService.ShowSuccess("Đã phát hành định mức thành công!"); }

    [RelayCommand(CanExecute = nameof(CanEditSelectedMaterial))]
    private async Task SaveRouting()
    {
        if (SelectedMaterial == null) return;
        var parent = await _masterDataService.GetMaterialByCodeAsync(SelectedMaterial.Id);
        if (parent == null) return;
        await _masterDataService.DeleteRoutingByProductIdAsync(parent.MaterialId);
        foreach (var step in RoutingSteps)
        {
            await _masterDataService.AddRoutingStepAsync(new Routing { ProductId = parent.MaterialId, StepNumber = step.StepNo, StepName = step.StepName, WorkCenter = step.WorkCenter, EstimatedTime = (int)step.StdTimeMinutes, OutputDescription = step.Output });
        }
        IsRoutingDirty = false;
        _notificationService.ShowSuccess("Lưu quy trình thành công.");
        _ = LoadAuditLogsAsync(SelectedMaterial.Id);
    }

    private bool CanEditSelectedMaterial() => SelectedMaterial?.CanHaveStructure ?? false;

    private async Task LoadBomAndRouting(MaterialItem? material)
    {
        BomLines.Clear(); RoutingSteps.Clear(); if (material is null) return;
        try
        {
            var boms = await _masterDataService.GetBomByParentCodeAsync(material.Id);
            foreach (var b in boms) BomLines.Add(new BomLineItem { Id = b.Bomid, ComponentId = b.ChildId ?? 0, ComponentCode = b.Child?.MaterialCode ?? "N/A", ComponentName = b.Child?.MaterialName ?? "N/A", QuantityPer = (double)b.QuantityPerUnit, Unit = b.Child?.Unit ?? "Cái" });
            var routings = await _masterDataService.GetRoutingByParentCodeAsync(material.Id);
            foreach (var r in routings) RoutingSteps.Add(new RoutingStepItem { StepNo = r.StepNumber ?? 0, StepName = r.StepName ?? "N/A", WorkCenter = r.WorkCenter ?? "N/A", StdTimeMinutes = r.EstimatedTime ?? 0, Output = r.OutputDescription ?? "N/A" });
            RecalculateTotalRoutingTime();
        }
        catch { }
    }

    [RelayCommand(CanExecute = nameof(CanEditSelectedMaterial))]
    private async Task AddBomLine()
    {
        if (SelectedMaterial == null) return;
        var materials = await _masterDataService.GetAllMaterialsAsync();
        var dialog = new Views.Dialogs.AddBomDialog(materials) { Owner = System.Windows.Application.Current?.MainWindow, ExistingChildIds = BomLines.Select(b => b.ComponentId).ToHashSet() };
        if (dialog.ShowDialog() == true && dialog.SelectedItems.Any())
        {
            var parent = await _masterDataService.GetMaterialByCodeAsync(SelectedMaterial.Id);
            if (parent == null) return;
            foreach (var selection in dialog.SelectedItems)
            {
                if (await _masterDataService.IsCircularReferenceAsync(parent.MaterialId, selection.MaterialId)) continue;
                await _masterDataService.AddBomItemAsync(new Bom { ParentId = parent.MaterialId, ChildId = selection.MaterialId, QuantityPerUnit = (decimal)selection.Quantity });
            }
            await LoadBomAndRouting(SelectedMaterial);
            _ = LoadAuditLogsAsync(SelectedMaterial.Id);
        }
    }

    [RelayCommand]
    private async Task EditBomLine(BomLineItem item)
    {
        if (item == null || SelectedMaterial == null) return;
        var dialog = new EditBomDialog(item.ComponentCode, item.ComponentName, item.QuantityPer, item.Unit) { Owner = System.Windows.Application.Current?.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            var success = await _masterDataService.UpdateBomItemAsync(new Bom { Bomid = item.Id, ParentId = SelectedMaterial.DbId, ChildId = item.ComponentId, QuantityPerUnit = (decimal)dialog.Quantity });
            if (success) { item.QuantityPer = dialog.Quantity; _ = LoadAuditLogsAsync(SelectedMaterial?.Id); }
        }
    }

    [RelayCommand]
    private async Task DeleteBomLine(BomLineItem item)
    {
        if (item == null) return;
        if (_notificationService.Confirm("Xóa linh kiện khỏi định mức?", "Xác nhận"))
        {
            if (await _masterDataService.DeleteBomItemAsync(item.Id)) { BomLines.Remove(item); _ = LoadAuditLogsAsync(SelectedMaterial?.Id); }
        }
    }

    private async void LoadMaterialHistory(MaterialItem? material)
    {
        MaterialHistory.Clear(); if (material == null) return;
        try
        {
            var transactions = await _warehouseService.GetTransactionsAsync();
            foreach (var t in transactions.Where(t => t.MaterialCode == material.Id)) MaterialHistory.Add(new InventoryHistoryItem { Date = DateTime.TryParse(t.TransactionDate, out var dt) ? dt : DateTime.Now, Type = t.Type, Quantity = t.Quantity, Warehouse = t.Warehouse, Reference = t.ReferenceDoc, User = t.TransBy });
        }
        catch { }
    }

    private void RecalculateTotalRoutingTime() => TotalRoutingTime = RoutingSteps.Sum(s => s.StdTimeMinutes);
}

public class MaterialItem : ObservableObject
{
    public int DbId { get; set; }
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _category = string.Empty;
    private string _unit = string.Empty;
    private string _status = string.Empty;
    private double _onHand;
    private double _minStock;
    private decimal _unitPrice;
    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }
    public string Unit { get => _unit; set => SetProperty(ref _unit, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public double OnHand { get => _onHand; set { if (SetProperty(ref _onHand, value)) { OnPropertyChanged(nameof(IsLowStock)); OnPropertyChanged(nameof(InventoryStatusLabel)); OnPropertyChanged(nameof(StockDisplay)); OnPropertyChanged(nameof(TotalValue)); } } }
    public double MinStock { get => _minStock; set { if (SetProperty(ref _minStock, value)) OnPropertyChanged(nameof(IsLowStock)); OnPropertyChanged(nameof(InventoryStatusLabel)); } }
    public decimal UnitPrice { get => _unitPrice; set { if (SetProperty(ref _unitPrice, value)) OnPropertyChanged(nameof(TotalValue)); } }
    public decimal TotalValue => (decimal)OnHand * UnitPrice;
    public bool IsLowStock => OnHand < MinStock && OnHand > 0;
    public bool IsOutOfStock => OnHand <= 0;
    public string InventoryStatusLabel => IsOutOfStock ? "Hết hàng" : (IsLowStock ? "Cảnh báo" : "Đủ");
    public string StockDisplay => $"{OnHand} / {MinStock}";
    public bool CanHaveStructure => Category == "Thành phẩm" || Category == "Bán thành phẩm";
}

public partial class BomLineItem : ObservableObject
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public double QuantityPer { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string QuantityDisplay => $"{QuantityPer} {Unit}";
}

public partial class RoutingStepItem : ObservableObject
{
    [ObservableProperty] private int _stepNo;
    [ObservableProperty] private string _stepName = string.Empty;
    [ObservableProperty] private string _workCenter = string.Empty;
    [ObservableProperty] private double _stdTimeMinutes;
    [ObservableProperty] private string _output = string.Empty;
}

public partial class PartnerItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _type = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
}

public class InventoryHistoryItem
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Warehouse { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}
