using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;

public partial class WarehouseReportViewModel : ViewModelBase
{
    private readonly IWarehouseService _warehouseService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;
    private readonly IAuthService _authService;
    private readonly IFileService _fileService;
    private readonly IActivityService _activityService;

    [ObservableProperty] private string _pageTitle = "Nhập / Xuất dữ liệu Kho bãi";
    [ObservableProperty] private bool _isExportMode = true;
    [ObservableProperty] private bool _isImportMode = false;
    
    // Export properties
    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private string _selectedReportType = "Tồn kho hiện tại";
    [ObservableProperty] private WarehouseConfig? _selectedWarehouse;
    [ObservableProperty] private bool _isExcelSelected = true;
    [ObservableProperty] private bool _isPdfSelected = false;

    // Import properties
    [ObservableProperty] private string _selectedFilePath = "Chưa chọn tệp tin...";
    [ObservableProperty] private bool _isUpdateMode = true;

    [ObservableProperty] private bool _isBusy;

    public string ActionButtonText => IsExportMode ? "Bắt đầu xuất báo cáo" : "Bắt đầu nhập dữ liệu";

    public ObservableCollection<string> ReportTypes { get; } = new() 
    { 
        "Tồn kho hiện tại", 
        "Nhật ký giao dịch (Nhập/Xuất)", 
        "Báo cáo Chuyển kho nội bộ",
        "Danh sách Cảnh báo tồn thấp" 
    };

    public ObservableCollection<WarehouseConfig> Warehouses { get; } = new();
    public ObservableCollection<ExportColumn> ExportColumns { get; } = new();
    public ObservableCollection<InstructionStep> Instructions { get; } = new();

    public WarehouseReportViewModel(
        IWarehouseService warehouseService,
        INavigationService navigationService,
        INotificationService notificationService,
        IAuthService authService,
        IFileService fileService,
        IActivityService activityService)
    {
        _warehouseService = warehouseService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _authService = authService;
        _fileService = fileService;
        _activityService = activityService;

        LoadInstructions();
    }

    partial void OnIsExportModeChanged(bool value)
    {
        OnPropertyChanged(nameof(ActionButtonText));
        LoadInstructions();
    }

    partial void OnIsImportModeChanged(bool value)
    {
        OnPropertyChanged(nameof(ActionButtonText));
        LoadInstructions();
    }

    public override async Task InitializeAsync()

    {
        await LoadWarehousesAsync();
        UpdateColumns();
    }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var whs = await _warehouseService.GetWarehousesAsync();
            App.Current.Dispatcher.Invoke(() =>
            {
                Warehouses.Clear();
                Warehouses.Add(new WarehouseConfig { Id = "all", Name = "Tất cả các kho" });
                foreach (var w in whs) Warehouses.Add(w);
                SelectedWarehouse = Warehouses[0];
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải danh sách kho: " + ex.Message);
        }
    }

    private void LoadInstructions()
    {
        Instructions.Clear();
        if (IsExportMode)
        {
            Instructions.Add(new InstructionStep { Title = "Bước 1: Chọn loại báo cáo", Content = "Báo cáo tồn kho cho biết số lượng hiện có. Nhật ký giao dịch cho biết các biến động nhập xuất." });
            Instructions.Add(new InstructionStep { Title = "Bước 2: Lọc dữ liệu", Content = "Chọn khoảng thời gian và kho cụ thể để thu hẹp phạm vi báo cáo." });
            Instructions.Add(new InstructionStep { Title = "Bước 3: Định dạng & Xuất", Content = "Chọn Excel nếu bạn muốn tính toán thêm, chọn PDF nếu muốn in ấn báo cáo ngay." });
        }
        else
        {
            Instructions.Add(new InstructionStep { Title = "Bước 1: Chuẩn bị file", Content = "Sử dụng file Excel có các cột: Mã vật tư, Tên vật tư, Đơn vị, Số lượng, Mã kho." });
            Instructions.Add(new InstructionStep { Title = "Bước 2: Chọn chế độ", Content = "Chọn 'Cập nhật' nếu bạn muốn cộng dồn tồn kho, hoặc 'Thêm mới' để ghi đè số liệu." });
            Instructions.Add(new InstructionStep { Title = "Bước 3: Kiểm tra & Nhập", Content = "Hệ thống sẽ kiểm tra tính hợp lệ của mã vật tư và kho trước khi thực hiện nhập." });
        }
    }

    partial void OnSelectedReportTypeChanged(string value)
    {
        UpdateColumns();
    }

    public class TransferExportModel
    {
        public string TransactionDate { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string SourceWarehouse { get; set; } = string.Empty;
        public string DestWarehouse { get; set; } = string.Empty;
        public string TransBy { get; set; } = string.Empty;
    }

    private void UpdateColumns()
    {
        ExportColumns.Clear();
        if (SelectedReportType == "Tồn kho hiện tại")
        {
            ExportColumns.Add(new ExportColumn { Name = "Mã vật tư", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Tên vật tư", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Đơn vị", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Số lượng tồn", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Kho", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Giá trị ước tính", IsSelected = true });
        }
        else if (SelectedReportType == "Nhật ký giao dịch (Nhập/Xuất)")
        {
            ExportColumns.Add(new ExportColumn { Name = "Ngày giao dịch", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Loại", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Mã vật tư", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Số lượng", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Kho", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Người thực hiện", IsSelected = true });
        }
        else if (SelectedReportType == "Báo cáo Chuyển kho nội bộ")
        {
            ExportColumns.Add(new ExportColumn { Name = "Ngày giao dịch", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Mã vật tư", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Tên vật tư", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Số lượng", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Kho nguồn", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Kho đích", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Người thực hiện", IsSelected = true });
        }
        else // Danh sách Cảnh báo tồn thấp
        {
            ExportColumns.Add(new ExportColumn { Name = "Mã vật tư", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Tên vật tư", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Kho", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Số lượng tồn", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Mức tối thiểu", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Số lượng thiếu", IsSelected = true });
            ExportColumns.Add(new ExportColumn { Name = "Mức độ cảnh báo", IsSelected = true });
        }
    }

    [RelayCommand]
    private void SelectFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel Files|*.xls;*.xlsx;*.xlsm",
            Title = "Chọn tệp tin dữ liệu kho"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task Process()
    {
        if (IsBusy) return;

        if (IsExportMode)
        {
            if (EndDate < StartDate)
            {
                _notificationService.ShowError("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
                return;
            }

            if (!ExportColumns.Any(c => c.IsSelected))
            {
                _notificationService.ShowError("Vui lòng chọn ít nhất một cột thông tin để xuất.");
                return;
            }

            try
            {
                IsBusy = true;
                string warehouseName = SelectedWarehouse?.Id == "all" ? "Tất cả" : (SelectedWarehouse?.Name ?? "Tất cả");
                
                object? exportData = null;
                string reportTitle = SelectedReportType.ToUpper();
                string fileNamePrefix = "Bao_cao_kho";

                if (SelectedReportType == "Tồn kho hiện tại")
                {
                    exportData = await _warehouseService.GetInventoryAsync(warehouseName);
                    fileNamePrefix = "Ton_kho_hien_tai";
                }
                else if (SelectedReportType == "Nhật ký giao dịch (Nhập/Xuất)")
                {
                    var allTrans = await _warehouseService.GetTransactionsAsync(warehouseName, 5000);
                    exportData = allTrans.Where(t => 
                    {
                        if (DateTime.TryParse(t.TransactionDate, out var dt))
                        {
                            return dt.Date >= StartDate.Date && dt.Date <= EndDate.Date;
                        }
                        return false;
                    }).ToList();
                    fileNamePrefix = "Nhat_ky_giao_dich";
                }
                else if (SelectedReportType == "Báo cáo Chuyển kho nội bộ")
                {
                    var allTrans = await _warehouseService.GetTransactionsAsync(warehouseName, 10000);
                    var warehouses = await _warehouseService.GetWarehousesAsync();
                    
                    // Filter and merge transfer pairs
                    exportData = allTrans
                        .Where(t => t.ReferenceDoc != null && t.ReferenceDoc.StartsWith("TRANSFER-OUT-TO-"))
                        .Select(t => {
                            string targetWhIdStr = t.ReferenceDoc!.Replace("TRANSFER-OUT-TO-", "");
                            var targetWh = warehouses.FirstOrDefault(w => w.Id == targetWhIdStr);
                            
                            return new TransferExportModel {
                                TransactionDate = t.TransactionDate,
                                MaterialCode = t.MaterialCode,
                                MaterialName = t.MaterialName,
                                Quantity = t.Quantity,
                                Unit = t.Unit,
                                SourceWarehouse = t.Warehouse,
                                DestWarehouse = targetWh?.Name ?? "Kho #" + targetWhIdStr,
                                TransBy = t.TransBy
                            };
                        })
                        .Where(t => {
                             if (DateTime.TryParse(t.TransactionDate, out var dt))
                             {
                                 return dt.Date >= StartDate.Date && dt.Date <= EndDate.Date;
                             }
                             return false;
                        })
                        .ToList();
                    fileNamePrefix = "Bao_cao_chuyen_kho";
                }
                else if (SelectedReportType == "Danh sách Cảnh báo tồn thấp")
                {
                    exportData = await _warehouseService.GetStockAlertsAsync();
                    fileNamePrefix = "Canh_bao_ton_kho";
                }

                if (exportData == null || !(exportData as System.Collections.IEnumerable).Cast<object>().Any())
                {
                    _notificationService.ShowError("Không có dữ liệu phù hợp với bộ lọc.");
                    return;
                }

                var filter = IsExcelSelected ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF Document (*.pdf)|*.pdf";
                var ext = IsExcelSelected ? "xlsx" : "pdf";
                var fileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmm}.{ext}";
                
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog { Filter = filter, FileName = fileName };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var selectedCols = ExportColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
                    bool success = false;

                    var dataType = exportData.GetType().GetGenericArguments()[0];

                    if (IsExcelSelected)
                    {
                        var method = _fileService.GetType().GetMethod("ExportToExcelAsync")?.MakeGenericMethod(dataType);
                        if (method != null)
                        {
                            var task = (Task<bool>)method.Invoke(_fileService, new object[] { exportData, saveFileDialog.FileName, "Báo cáo", selectedCols, reportTitle });
                            success = await task;
                        }
                    }
                    else
                    {
                        var method = _fileService.GetType().GetMethod("ExportToPdfAsync")?.MakeGenericMethod(dataType);
                        if (method != null)
                        {
                            var task = (Task<bool>)method.Invoke(_fileService, new object[] { exportData, saveFileDialog.FileName, reportTitle, selectedCols });
                            success = await task;
                        }
                    }

                    if (success) 
                    {
                        _notificationService.ShowSuccess("Xuất báo cáo thành công!");
                        // Log activity
                        await _activityService.LogActivityAsync("Xuất báo cáo", $"[Warehouse] Xuất {SelectedReportType} ra file {(IsExcelSelected ? "Excel" : "PDF")}");
                    }
                    else _notificationService.ShowError("Lỗi khi lưu file.");
                }
            }
            catch (Exception ex) { _notificationService.ShowError("Lỗi: " + ex.Message); }
            finally { IsBusy = false; }
        }
        else // IMPORT
        {
             if (string.IsNullOrEmpty(SelectedFilePath) || SelectedFilePath == "Chưa chọn tệp tin...")
            {
                _notificationService.ShowError("Vui lòng chọn tệp tin Excel.");
                return;
            }

            try
            {
                IsBusy = true;
                var importedData = await _fileService.ImportFromExcelAsync<InventoryItemDisplay>(SelectedFilePath);
                
                if (importedData == null || !importedData.Any())
                {
                    _notificationService.ShowError("File không có dữ liệu.");
                    return;
                }

                int count = 0;
                var materials = await _warehouseService.GetAllMaterialsAsync();
                var warehouses = await _warehouseService.GetWarehousesAsync();

                foreach (var item in importedData)
                {
                    var mat = materials.FirstOrDefault(m => m.MaterialCode == item.MaterialCode);
                    var wh = warehouses.FirstOrDefault(w => w.Name == item.Warehouse || w.Code == item.Warehouse);

                    if (mat != null && wh != null && int.TryParse(wh.Id, out int whId))
                    {
                        int userId = _authService.CurrentUser?.UserId ?? 1;
                        bool ok = await _warehouseService.AdjustStockAsync(whId, mat.MaterialId, (decimal)item.CurrentQty, userId, "Nhập dữ liệu từ Excel", IsUpdateMode);
                        if (ok) count++;
                    }
                }

                _notificationService.ShowSuccess($"Đã nạp {count} dòng dữ liệu tồn kho!");
                
                // Log activity
                await _activityService.LogActivityAsync("Nhập file", $"[Warehouse] Nhập dữ liệu tồn kho từ file Excel. Số dòng: {count}");

                var whVm = _navigationService.NavigateTo<WarehouseViewModel>();
                await whVm.InitializeAsync();
            }
            catch (Exception ex) { _notificationService.ShowError("Lỗi: " + ex.Message); }
            finally { IsBusy = false; }
        }
    }

    [RelayCommand]
    private async Task Back()
    {
        var vm = _navigationService.NavigateTo<WarehouseViewModel>();
        await vm.InitializeAsync();
    }
}

public partial class ExportColumn : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isSelected = true;
}

public class InstructionStep
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
