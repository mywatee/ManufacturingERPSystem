using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ManufacturingERP.ViewModels;

public partial class CreateMaterialViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IMasterDataService _masterDataService;
    private readonly IWarehouseService _warehouseService;
    private readonly IAuthService _authService;

    public ObservableCollection<BomItemViewModel> BomItems { get; } = new();

    [ObservableProperty] private string _materialCode = "";
    [ObservableProperty] private string _materialName = "";
    [ObservableProperty] private string _unit = "Kg";
    [ObservableProperty] private string _category = "Nguyên liệu";
    [ObservableProperty] private string _status = "Đang sử dụng";
    [ObservableProperty] private int _minStock = 10;
    [ObservableProperty] private decimal _unitPrice = 0;
    [ObservableProperty] private int _initialStock = 0;
    [ObservableProperty] private bool _isTechnicalDataVisible = false;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private bool _isEditMode = false;
    [ObservableProperty] private string _pageTitle = "Thêm vật tư mới";

    public ObservableCollection<string> Categories { get; } = new() { "Nguyên liệu", "Bán thành phẩm", "Thành phẩm", "Linh kiện", "Vật tư tiêu hao" };
    public ObservableCollection<string> Statuses { get; } = new() { "Đang sử dụng", "Ngừng sử dụng" };

    public CreateMaterialViewModel(
        INavigationService navigationService, 
        INotificationService notificationService,
        IMasterDataService masterDataService,
        IWarehouseService warehouseService,
        IAuthService authService)
    {
        _navigationService = navigationService;
        _notificationService = notificationService;
        _masterDataService = masterDataService;
        _warehouseService = warehouseService;
        _authService = authService;
        
        // Default for new material
        MaterialCode = "VT-" + DateTime.Now.ToString("yyMMddHHmm");
        UpdateTechnicalVisibility();
    }

    public async Task LoadMaterialAsync(string code)
    {
        try
        {
            IsBusy = true;
            IsEditMode = true;
            PageTitle = "Cập nhật thông tin vật tư";

            var material = await _masterDataService.GetMaterialByCodeAsync(code);
            if (material != null)
            {
                MaterialCode = material.MaterialCode;
                MaterialName = material.MaterialName;
                Unit = material.Unit ?? "Cái";
                Category = material.Category ?? "Nguyên liệu";
                Status = material.Status ?? "Đang sử dụng";
                MinStock = material.MinStock ?? 0;
                UnitPrice = material.UnitPrice ?? 0;
                
                // Load BOM
                BomItems.Clear();
                var boms = await _masterDataService.GetBomByParentCodeAsync(code);
                foreach (var b in boms)
                {
                    BomItems.Add(new BomItemViewModel
                    {
                        MaterialId = b.ChildId.ToString(),
                        MaterialCode = b.Child?.MaterialCode ?? "N/A",
                        MaterialName = b.Child?.MaterialName ?? "N/A",
                        Quantity = (double)b.QuantityPerUnit,
                        Unit = b.Child?.Unit ?? "Cái"
                    });
                }
            }
            UpdateTechnicalVisibility();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải dữ liệu vật tư: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnCategoryChanged(string value) => UpdateTechnicalVisibility();

    private void UpdateTechnicalVisibility()
    {
        IsTechnicalDataVisible = Category == "Thành phẩm" || Category == "Bán thành phẩm";
    }

    [RelayCommand]
    private async Task AddBomItem()
    {
        var materials = await _masterDataService.GetAllMaterialsAsync();
        var dialog = new Views.Dialogs.AddBomDialog(materials)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.SelectedItems.Any())
        {
            foreach (var selection in dialog.SelectedItems)
            {
                var mat = selection.Material;
                BomItems.Add(new BomItemViewModel
                {
                    MaterialId = mat.MaterialId.ToString(),
                    MaterialCode = mat.MaterialCode,
                    MaterialName = mat.MaterialName,
                    Quantity = selection.Quantity,
                    Unit = mat.Unit ?? "Cái"
                });
            }
        }
    }

    [RelayCommand]
    private void RemoveBomItem(BomItemViewModel item)
    {
        if (item != null) BomItems.Remove(item);
    }

    [RelayCommand]
    private void Cancel() => _navigationService.NavigateTo<MasterDataViewModel>();

    [RelayCommand]
    private async Task Save()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(MaterialCode))
        {
            _notificationService.ShowError("Vui lòng nhập Mã vật tư.");
            return;
        }

        if (MaterialCode.Any(char.IsWhiteSpace))
        {
            _notificationService.ShowError("Mã vật tư không được chứa khoảng trắng.");
            return;
        }

        if (string.IsNullOrWhiteSpace(MaterialName))
        {
            _notificationService.ShowError("Vui lòng nhập Tên vật tư.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Unit))
        {
            _notificationService.ShowError("Vui lòng nhập Đơn vị tính.");
            return;
        }

        if (MinStock < 0)
        {
            _notificationService.ShowError("Số lượng tồn tối thiểu không được âm.");
            return;
        }

        if (IsTechnicalDataVisible && !BomItems.Any())
        {
            if (!_notificationService.Confirm("Vật tư này là Thành phẩm/Bán thành phẩm nhưng chưa có định mức (BOM). Bạn vẫn muốn tiếp tục lưu?", "Cảnh báo thiếu BOM"))
            {
                return;
            }
        }

        try
        {
            IsBusy = true;
            Material? material;

            if (IsEditMode)
            {
                material = await _masterDataService.GetMaterialByCodeAsync(MaterialCode);
                if (material == null) return;

                material.MaterialName = MaterialName.Trim();
                material.Unit = Unit;
                material.Category = Category;
                material.Status = Status;
                material.MinStock = MinStock;
                material.UnitPrice = UnitPrice;

                await _masterDataService.UpdateMaterialAsync(material);
            }
            else
            {
                if (await _masterDataService.IsMaterialCodeExistsAsync(MaterialCode))
                {
                    _notificationService.ShowError($"Mã '{MaterialCode}' đã tồn tại.");
                    return;
                }

                material = new Material
                {
                    MaterialCode = MaterialCode.Trim().ToUpper(),
                    MaterialName = MaterialName.Trim(),
                    Unit = Unit,
                    Category = Category,
                    Status = Status,
                    MinStock = MinStock,
                    UnitPrice = UnitPrice,
                    CreatedAt = DateTime.Now
                };
                await _masterDataService.AddMaterialAsync(material);

                // Handle Initial Stock for new materials
                if (InitialStock > 0)
                {
                    var warehouses = await _warehouseService.GetWarehousesAsync();
                    if (warehouses.Any())
                    {
                        var targetWh = warehouses.First();
                        if (int.TryParse(targetWh.Id, out int whId))
                        {
                            await _warehouseService.AddStockTransactionAsync(new StockTransaction
                            {
                                MaterialId = material.MaterialId,
                                WarehouseId = whId,
                                Quantity = InitialStock,
                                Type = "Nhập kho",
                                ReferenceCode = "TON_DAU_KY",
                                TransBy = _authService.CurrentUser?.UserId,
                                TransDate = DateTime.Now,
                                Notes = "Khởi tạo tồn kho ban đầu"
                            });
                        }
                    }
                    else
                    {
                        _notificationService.ShowError("Vật tư đã được tạo, nhưng không thể thiết lập tồn kho ban đầu vì chưa có nhà kho nào được định nghĩa.");
                    }
                }
            }

            // Sync BOM Items (For simplicity in Edit mode, we delete all and re-add)
            // In a real app, you'd compare and do incremental updates.
            var savedMaterial = await _masterDataService.GetMaterialByCodeAsync(MaterialCode);
            if (savedMaterial != null)
            {
                // Clear old BOMs if editing
                if (IsEditMode)
                {
                    var oldBoms = await _masterDataService.GetBomByParentCodeAsync(MaterialCode);
                    foreach(var ob in oldBoms) await _masterDataService.DeleteBomItemAsync(ob.Bomid);
                }

                foreach (var item in BomItems)
                {
                    int childId = int.Parse(item.MaterialId);
                    if (await _masterDataService.IsCircularReferenceAsync(savedMaterial.MaterialId, childId))
                    {
                        continue; // Skip or notify
                    }

                    await _masterDataService.AddBomItemAsync(new Bom
                    {
                        ParentId = savedMaterial.MaterialId,
                        ChildId = childId,
                        QuantityPerUnit = (decimal)item.Quantity
                    });
                }
            }

            _notificationService.ShowSuccess(IsEditMode ? "Cập nhật thành công!" : "Thêm mới thành công!");
            
            var masterDataVM = ((App)System.Windows.Application.Current).Services.GetService(typeof(MasterDataViewModel)) as MasterDataViewModel;
            if (masterDataVM != null) await masterDataVM.LoadMaterialsAsync();

            _navigationService.NavigateTo<MasterDataViewModel>();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi hệ thống: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
