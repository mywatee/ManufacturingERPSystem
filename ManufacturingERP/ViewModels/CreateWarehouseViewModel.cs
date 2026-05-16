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

namespace ManufacturingERP.ViewModels;

public partial class CreateWarehouseViewModel : ViewModelBase
{
    private readonly IWarehouseService _warehouseService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IAuthService _authService;
    private readonly IAuditLogService _auditLogService;

    [ObservableProperty] private string _warehouseCode = "";
    [ObservableProperty] private string _warehouseName = "";
    [ObservableProperty] private string _location = "";
    [ObservableProperty] private string _capacity = "0";
    [ObservableProperty] private string _capacityUnit = "m²";
    [ObservableProperty] private string _status = "Hoạt động";
    [ObservableProperty] private string _warehouseType = "Kho chung";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _contactPhone = "";
    [ObservableProperty] private string _contactEmail = "";
    [ObservableProperty] private string _safetyThreshold = "90";
    [ObservableProperty] private User? _selectedManager;
    [ObservableProperty] private bool _isAutoCode;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private string _pageTitle = "Thêm nhà kho mới";

    private int _editingWarehouseId;

    public ObservableCollection<User> Managers { get; } = new();
    public ObservableCollection<string> StatusList { get; } = new() { "Hoạt động", "Bảo trì", "Đã đóng" };
    public ObservableCollection<string> WarehouseTypes { get; } = new() { "Kho nguyên vật liệu", "Kho thành phẩm", "Kho bán thành phẩm", "Kho linh kiện", "Kho vật tư tiêu hao", "Kho lạnh", "Kho hàng hỏng", "Kho chung" };
    public ObservableCollection<string> UnitList { get; } = new() { "m²", "m³", "Pallet", "Kệ" };

    public CreateWarehouseViewModel(
        IWarehouseService warehouseService,
        INavigationService navigationService,
        INotificationService notificationService,
        IAuthService authService,
        IAuditLogService auditLogService)
    {
        _warehouseService = warehouseService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _authService = authService;
        _auditLogService = auditLogService;
    }

    public override async Task InitializeAsync() => await InitializeAsync(null);

    public async Task InitializeAsync(int? warehouseId, bool readOnly = false)
    {
        await LoadManagersAsync();
        IsReadOnly = readOnly;
        
        if (warehouseId.HasValue)
        {
            IsEditMode = true;
            _editingWarehouseId = warehouseId.Value;
            PageTitle = readOnly ? "Chi tiết nhà kho" : "Chỉnh sửa nhà kho";
            await LoadWarehouseDataAsync(warehouseId.Value);
        }
        else
        {
            IsEditMode = false;
            PageTitle = "Thêm nhà kho mới";
            ClearForm();
        }
    }

    private async Task LoadWarehouseDataAsync(int id)
    {
        try
        {
            using var context = new ManufacturingContext();
            var w = await context.Warehouses.Include(x => x.Manager).FirstOrDefaultAsync(x => x.WarehouseId == id);
            if (w != null)
            {
                WarehouseCode = w.Code ?? "";
                WarehouseName = w.WarehouseName ?? "";
                Location = w.Location ?? "";
                Capacity = w.Capacity?.ToString() ?? "0";
                CapacityUnit = w.CapacityUnit ?? "m²";
                Status = w.Status ?? "Hoạt động";
                WarehouseType = w.WarehouseType ?? "Kho chung";
                Description = w.Description ?? "";
                ContactPhone = w.ContactPhone ?? "";
                ContactEmail = w.ContactEmail ?? "";
                SafetyThreshold = w.SafetyThreshold?.ToString() ?? "90";
                SelectedManager = Managers.FirstOrDefault(m => m.UserId == w.ManagerId);
                IsAutoCode = false;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi tải dữ liệu nhà kho: " + ex.Message);
        }
    }

    private void ClearForm()
    {
        WarehouseCode = "";
        WarehouseName = "";
        Location = "";
        Capacity = "0";
        CapacityUnit = "m²";
        Status = "Hoạt động";
        WarehouseType = "Kho chung";
        Description = "";
        ContactPhone = "";
        ContactEmail = "";
        SafetyThreshold = "90";
        SelectedManager = null;
        IsAutoCode = false;
    }

    partial void OnIsAutoCodeChanged(bool value)
    {
        if (value)
        {
            WarehouseCode = $"WH-{DateTime.Now:MMdd-HHmm}";
        }
        else if (WarehouseCode.StartsWith("WH-"))
        {
            WarehouseCode = "";
        }
    }

    private async Task LoadManagersAsync()

    {
        try
        {
            using var context = new ManufacturingContext();
            var managers = await context.Users.Where(u => u.IsActive == true).ToListAsync();
            App.Current.Dispatcher.Invoke(() =>
            {
                Managers.Clear();
                foreach (var m in managers)
                {
                    Managers.Add(m);
                }
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi tải danh sách quản lý: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(WarehouseName))
        {
            _notificationService.ShowError("Vui lòng nhập tên nhà kho.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            _notificationService.ShowError("Vui lòng nhập vị trí nhà kho.");
            return;
        }

        if (!decimal.TryParse(Capacity, out decimal cap) || cap <= 0)
        {
            _notificationService.ShowError("Vui lòng nhập công suất tối đa lớn hơn 0.");
            return;
        }

        if (!decimal.TryParse(SafetyThreshold, out decimal threshold) || threshold < 0 || threshold > 100)
        {
            _notificationService.ShowError("Ngưỡng an toàn phải nằm trong khoảng 0 - 100%.");
            return;
        }

        if (SelectedManager == null)
        {
            _notificationService.ShowError("Vui lòng chọn người quản lý kho.");
            return;
        }

        // Validate Email
        if (!string.IsNullOrWhiteSpace(ContactEmail))
        {
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(ContactEmail))
            {
                _notificationService.ShowError("Định dạng email không hợp lệ.");
                return;
            }
        }

        // Validate Phone (basic)
        if (!string.IsNullOrWhiteSpace(ContactPhone))
        {
            if (ContactPhone.Length < 8)
            {
                _notificationService.ShowError("Số điện thoại không hợp lệ.");
                return;
            }
        }

        string code = WarehouseCode?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(code))
        {
            _notificationService.ShowError("Vui lòng nhập mã nhà kho hoặc chọn Tạo tự động.");
            return;
        }



        // Check for duplicate code (if not auto-generated or if edited)
        using (var context = new ManufacturingContext())
        {
            var existing = await context.Warehouses
                .AnyAsync(w => w.Code == code && w.WarehouseId != (IsEditMode ? _editingWarehouseId : 0));
            if (existing)
            {
                _notificationService.ShowError("Mã nhà kho này đã tồn tại trong hệ thống. Vui lòng chọn mã khác.");
                return;
            }
        }

        var warehouse = new Warehouse
        {
            WarehouseId = IsEditMode ? _editingWarehouseId : 0,
            Code = code,
            WarehouseName = WarehouseName.Trim(),
            Location = Location.Trim(),
            Capacity = cap,
            CapacityUnit = CapacityUnit,
            Status = Status,
            WarehouseType = WarehouseType,
            Description = Description?.Trim(),
            ContactPhone = ContactPhone?.Trim(),
            ContactEmail = ContactEmail?.Trim(),
            SafetyThreshold = threshold,
            ManagerId = SelectedManager?.UserId
        };

        bool success;
        if (IsEditMode)
        {
            success = await _warehouseService.UpdateWarehouseAsync(warehouse);
        }
        else
        {
            success = await _warehouseService.AddWarehouseAsync(warehouse);
        }

        if (success)
        {
            string action = IsEditMode ? "Cập nhật" : "Thêm";
            await _auditLogService.LogAsync(_authService.CurrentUser?.UserId, action, "Warehouses", IsEditMode ? _editingWarehouseId.ToString() : null, warehouse.WarehouseName);
            _notificationService.ShowSuccess($"Đã {action.ToLower()} nhà kho thành công.");
            var vm = _navigationService.NavigateTo<WarehouseViewModel>();
            await vm.InitializeAsync();
        }
        else
        {
            _notificationService.ShowError("Có lỗi xảy ra khi lưu nhà kho.");
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        var vm = _navigationService.NavigateTo<WarehouseViewModel>();
        await vm.InitializeAsync();
    }
}
