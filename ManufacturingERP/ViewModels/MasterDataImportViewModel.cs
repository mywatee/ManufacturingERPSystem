using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Services;
using ManufacturingERP.Models;
using ManufacturingERP.Core;
using Microsoft.Win32;

namespace ManufacturingERP.ViewModels;

public partial class MasterDataImportViewModel : ViewModelBase
{
    public class MaterialImportModel
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public double MinStock { get; set; }
    }

    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IMasterDataService _masterDataService;
    private readonly INavigationService _navigationService;
    private readonly IActivityService _activityService;
    private readonly IAccessControlService _accessControlService;

    [ObservableProperty] private string _selectedFilePath = "Chưa chọn tệp...";
    [ObservableProperty] private bool _isUpdateMode = false;

    public List<InstructionItem> Instructions { get; } = new();

    [RelayCommand]
    private async Task DownloadTemplate()
    {
        try
        {
            var sampleData = new List<MaterialImportModel>
            {
                new MaterialImportModel { Code = "MAT-001", Name = "Thép tấm A36", Category = "Nguyên liệu", Unit = "Kg", MinStock = 100 },
                new MaterialImportModel { Code = "PROD-001", Name = "Bàn làm việc gỗ", Category = "Thành phẩm", Unit = "Cái", MinStock = 10 },
                new MaterialImportModel { Code = "SUB-001", Name = "Chân bàn thép", Category = "Bán thành phẩm", Unit = "Cái", MinStock = 40 }
            };

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "Mau_nhap_lieu_vattu.xlsx",
                Title = "Lưu file mẫu nhập liệu"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var headers = new Dictionary<string, string>
                {
                    { "Code", "Mã vật tư" },
                    { "Name", "Tên vật tư" },
                    { "Category", "Phân loại" },
                    { "Unit", "Đơn vị tính" },
                    { "MinStock", "Tồn tối thiểu" }
                };

                bool success = await _fileService.GenerateImportTemplateAsync(sampleData, saveFileDialog.FileName, "Dữ liệu mẫu", headers);
                if (success)
                {
                    _notificationService.ShowSuccess("Đã tải file mẫu thành công!");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi khi tải mẫu: {ex.Message}");
        }
    }

    public MasterDataImportViewModel(
        IMasterDataService masterDataService, 
        INavigationService navigationService, 
        IFileService fileService,
        INotificationService notificationService,
        IActivityService activityService,
        IAccessControlService accessControlService)
    {
        _masterDataService = masterDataService;
        _navigationService = navigationService;
        _fileService = fileService;
        _notificationService = notificationService;
        _activityService = activityService;
        _accessControlService = accessControlService;
        
        LoadInstructions();
    }

    private void LoadInstructions()
    {
        Instructions.Clear();
        Instructions.Add(new InstructionItem("Cấu trúc File", "Các cột bắt buộc: Mã vật tư, Tên vật tư, Phân loại (Nguyên liệu/Thành phẩm/Bán thành phẩm)."));
        Instructions.Add(new InstructionItem("Xử lý trùng lặp", "Mã vật tư là duy nhất. Bật 'Chế độ cập nhật' để ghi đè thông tin vật tư cũ."));
        Instructions.Add(new InstructionItem("Định dạng số", "Cột 'Tồn tối thiểu' phải là định dạng số. Hệ thống sẽ bỏ qua nếu không hợp lệ."));
    }

    [RelayCommand]
    private async Task Back() 
    {
        var vm = _navigationService.NavigateTo<MasterDataViewModel>();
        await vm.LoadMaterialsAsync();
    }

    [RelayCommand]
    private void SelectFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            Title = "Chọn file Excel nguồn để nhập vật tư"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            SelectedFilePath = openFileDialog.FileName;
        }
    }

    [RelayCommand]
    private async Task Process()
    {
        if (IsBusy) return;
        
        try 
        {
            var requiredAction = IsUpdateMode ? PermissionAction.Edit : PermissionAction.Add;

            if (!await _accessControlService.HasAsync(SystemModules.MasterData, requiredAction))
            {
                _notificationService.ShowError($"Bạn không có quyền thực hiện thao tác này.");
                return;
            }

            if (string.IsNullOrEmpty(SelectedFilePath) || SelectedFilePath == "Chưa chọn tệp...")
            {
                _notificationService.ShowError("Vui lòng chọn file Excel trước khi bắt đầu.");
                return;
            }

            IsBusy = true;
            _notificationService.ShowInfo("Đang đọc dữ liệu từ Excel...");

            var importedData = await _fileService.ImportFromExcelAsync<MaterialImportModel>(SelectedFilePath);
            
            if (importedData == null || !importedData.Any())
            {
                _notificationService.ShowError("Không tìm thấy dữ liệu hợp lệ trong file Excel.");
                return;
            }

            int successCount = 0;
            int updateCount = 0;
            int skippedCount = 0;

            foreach (var item in importedData)
            {
                if (string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Name))
                {
                    skippedCount++;
                    continue;
                }

                // Smart mapping for Category
                string category = "Nguyên liệu";
                if (!string.IsNullOrWhiteSpace(item.Category))
                {
                    var catLower = item.Category.ToLower();
                    if (catLower.Contains("thành phẩm") && !catLower.Contains("bán")) category = "Thành phẩm";
                    else if (catLower.Contains("bán thành phẩm")) category = "Bán thành phẩm";
                }

                var existing = await _masterDataService.GetMaterialByCodeAsync(item.Code);

                if (existing != null)
                {
                    if (IsUpdateMode)
                    {
                        existing.MaterialName = item.Name;
                        existing.Category = category;
                        existing.Unit = item.Unit ?? existing.Unit;
                        existing.MinStock = (int)item.MinStock;

                        if (await _masterDataService.UpdateMaterialAsync(existing))
                        {
                            updateCount++;
                        }
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                else
                {
                    var newMaterial = new Material
                    {
                        MaterialCode = item.Code,
                        MaterialName = item.Name,
                        Category = category,
                        Unit = item.Unit ?? "Cái",
                        MinStock = (int)item.MinStock,
                        Status = "Đang sử dụng"
                    };

                    if (await _masterDataService.AddMaterialAsync(newMaterial))
                    {
                        successCount++;
                    }
                }
            }

            await _activityService.LogActivityAsync("Hệ thống", $"Nhập Excel vật tư: Thêm mới {successCount}, Cập nhật {updateCount}");
            
            string resultMsg = $"Hoàn tất nhập dữ liệu!";
            if (successCount > 0) resultMsg += $"\n- Thêm mới: {successCount}";
            if (updateCount > 0) resultMsg += $"\n- Cập nhật: {updateCount}";
            if (skippedCount > 0) resultMsg += $"\n- Bỏ qua: {skippedCount} (do trùng mã hoặc thiếu thông tin)";

            _notificationService.ShowSuccess(resultMsg);
            
            if (successCount > 0 || updateCount > 0)
            {
                Back();
            }
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
