using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Services;
using ManufacturingERP.Models;
using ManufacturingERP.Core;
using Microsoft.Win32;

namespace ManufacturingERP.ViewModels;

public partial class MasterDataImportExportViewModel : ViewModelBase
{
    public class MaterialImportExportModel
    {
        public string? MaterialCode { get; set; }
        public string? MaterialName { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public double MinStock { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Status { get; set; }
    }

    public class BomImportExportModel
    {
        public string? ParentCode { get; set; }
        public string? ParentName { get; set; }
        public string? ChildCode { get; set; }
        public string? ChildName { get; set; }
        public decimal BomQuantity { get; set; }
        public string? Unit { get; set; }
    }

    public class RoutingImportExportModel
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int? StepNumber { get; set; }
        public string? StepName { get; set; }
        public string? WorkCenter { get; set; }
        public int? EstimatedTime { get; set; }
        public string? OutputDescription { get; set; }
    }

    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IMasterDataService _masterDataService;
    private readonly INavigationService _navigationService;
    private readonly IActivityService _activityService;
    private readonly IAccessControlService _accessControlService;

    [ObservableProperty] private string _selectedFilePath = "Chưa chọn tệp...";
    [ObservableProperty] private bool _isUpdateMode = false;
    [ObservableProperty] private bool _isExportMode = false;
    [ObservableProperty] private bool _isImportMode = true;

    // Export Filters
    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;
    [ObservableProperty] private string _selectedStatus = "Tất cả trạng thái";
    [ObservableProperty] private bool _isExcelFormat = true;
    [ObservableProperty] private bool _isPdfFormat = false;

    public ObservableCollection<string> Statuses { get; } = new() 
    { 
        "Tất cả trạng thái", "Đang sử dụng", "Sắp hết", "Hết hàng", "Ngừng sử dụng" 
    };

    [ObservableProperty] private string _selectedDataType = "Vật tư";
    public ObservableCollection<string> DataTypes { get; } = new() { "Vật tư", "Định mức (BOM)", "Quy trình (Routing)" };

    public string PageTitle => $"{ (IsExportMode ? "Xuất" : "Nhập") } {SelectedDataType}";
    public string ActionButtonText => IsExportMode ? "Bắt đầu xuất file" : "Bắt đầu nạp dữ liệu";

    public List<InstructionItem> Instructions { get; } = new();
    public ObservableCollection<ColumnSelectionItem> ExportColumns { get; } = new();

    public MasterDataImportExportViewModel(
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
        LoadDefaultColumns();
    }

    private void LoadDefaultColumns()
    {
        ExportColumns.Clear();
        if (SelectedDataType == "Vật tư")
        {
            ExportColumns.Add(new ColumnSelectionItem("Mã vật tư", true));
            ExportColumns.Add(new ColumnSelectionItem("Tên vật tư", true));
            ExportColumns.Add(new ColumnSelectionItem("Phân loại", true));
            ExportColumns.Add(new ColumnSelectionItem("Đơn vị", true));
            ExportColumns.Add(new ColumnSelectionItem("Mức tối thiểu", true));
            ExportColumns.Add(new ColumnSelectionItem("Đơn giá", true));
            ExportColumns.Add(new ColumnSelectionItem("Trạng thái hiện tại", true));
        }
        else if (SelectedDataType == "Định mức (BOM)")
        {
            ExportColumns.Add(new ColumnSelectionItem("Mã sản phẩm chính", true));
            ExportColumns.Add(new ColumnSelectionItem("Tên sản phẩm chính", true));
            ExportColumns.Add(new ColumnSelectionItem("Mã linh kiện", true));
            ExportColumns.Add(new ColumnSelectionItem("Tên linh kiện", true));
            ExportColumns.Add(new ColumnSelectionItem("Số lượng định mức", true));
            ExportColumns.Add(new ColumnSelectionItem("Đơn vị", true));
        }
        else // Routing
        {
            ExportColumns.Add(new ColumnSelectionItem("Mã sản phẩm", true));
            ExportColumns.Add(new ColumnSelectionItem("Thứ tự bước", true));
            ExportColumns.Add(new ColumnSelectionItem("Tên công đoạn", true));
            ExportColumns.Add(new ColumnSelectionItem("Trung tâm làm việc", true));
            ExportColumns.Add(new ColumnSelectionItem("Thời gian chuẩn (phút)", true));
            ExportColumns.Add(new ColumnSelectionItem("Mô tả đầu ra", true));
        }
    }

    partial void OnIsExportModeChanged(bool value)
    {
        IsImportMode = !value;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ActionButtonText));
        LoadInstructions();
    }

    partial void OnIsImportModeChanged(bool value)
    {
        IsExportMode = !value;
    }

    private void LoadInstructions()
    {
        Instructions.Clear();
        if (IsImportMode)
        {
            if (SelectedDataType == "Vật tư")
                Instructions.Add(new InstructionItem("Cấu trúc File", "Các cột: Mã vật tư, Tên, Phân loại, Đơn vị, Tồn tối thiểu, Đơn giá, Status."));
            else if (SelectedDataType == "Định mức (BOM)")
                Instructions.Add(new InstructionItem("Cấu trúc File", "Các cột: Mã sản phẩm chính, Mã linh kiện, Số lượng."));
            else
                Instructions.Add(new InstructionItem("Cấu trúc File", "Các cột: Mã sản phẩm, Thứ tự, Tên công đoạn, Trung tâm, Thời gian."));

            Instructions.Add(new InstructionItem("Ràng buộc", "Mã sản phẩm/vật tư phải tồn tại trước khi nạp BOM hoặc Quy trình."));
        }
        else
        {
            Instructions.Add(new InstructionItem("Định dạng xuất", $"Dữ liệu {SelectedDataType} sẽ được xuất ra Excel/PDF chuẩn."));
            Instructions.Add(new InstructionItem("Tùy chọn", "Tích chọn các cột cần thiết trước khi bắt đầu xuất."));
        }
    }

    partial void OnSelectedDataTypeChanged(string value)
    {
        OnPropertyChanged(nameof(PageTitle));
        LoadDefaultColumns();
        LoadInstructions();
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
    private async Task DownloadTemplate()
    {
        try
        {
            object sampleData = null;
            Dictionary<string, string> headers = null;
            string fileName = "";

            if (SelectedDataType == "Vật tư")
            {
                sampleData = new List<MaterialImportExportModel>
                {
                    new MaterialImportExportModel { MaterialCode = "MAT-001", MaterialName = "Thép tấm A36", Category = "Nguyên liệu", Unit = "Kg", MinStock = 100, UnitPrice = 50000, Status = "Đang sử dụng" },
                    new MaterialImportExportModel { MaterialCode = "PROD-001", MaterialName = "Bàn làm việc gỗ", Category = "Thành phẩm", Unit = "Cái", MinStock = 10, UnitPrice = 2000000, Status = "Đang sử dụng" }
                };
                headers = new Dictionary<string, string> 
                { 
                    { "MaterialCode", "Mã vật tư" }, 
                    { "MaterialName", "Tên vật tư" }, 
                    { "Category", "Phân loại" }, 
                    { "Unit", "Đơn vị tính" }, 
                    { "MinStock", "Tồn tối thiểu" },
                    { "UnitPrice", "Đơn giá" },
                    { "Status", "Status" }
                };
                fileName = "Mau_nhap_lieu_vattu.xlsx";
            }
            else if (SelectedDataType == "Định mức (BOM)")
            {
                sampleData = new List<BomImportExportModel>
                {
                    new BomImportExportModel 
                    { 
                        ParentCode = "PROD-001", 
                        ParentName = "Bàn làm việc gỗ (Mẫu)", 
                        ChildCode = "MAT-001", 
                        ChildName = "Thép tấm A36 (Mẫu)", 
                        BomQuantity = 4,
                        Unit = "Kg"
                    }
                };
                headers = new Dictionary<string, string> { { "ParentCode", "Mã sản phẩm chính" }, { "ChildCode", "Mã linh kiện" }, { "BomQuantity", "Số lượng định mức" } };
                fileName = "Mau_nhap_lieu_BOM.xlsx";
            }
            else // Routing
            {
                sampleData = new List<RoutingImportExportModel>
                {
                    new RoutingImportExportModel 
                    { 
                        ProductCode = "PROD-001", 
                        ProductName = "Bàn làm việc gỗ (Mẫu)",
                        StepNumber = 1, 
                        StepName = "Cắt phôi", 
                        WorkCenter = "Máy Cắt CNC", 
                        EstimatedTime = 15,
                        OutputDescription = "Phôi tấm đã cắt đúng kích thước"
                    }
                };
                headers = new Dictionary<string, string> { { "ProductCode", "Mã sản phẩm" }, { "StepNumber", "Thứ tự bước" }, { "StepName", "Tên công đoạn" }, { "WorkCenter", "Trung tâm làm việc" }, { "EstimatedTime", "Thời gian chuẩn (phút)" }, { "OutputDescription", "Mô tả đầu ra" } };
                fileName = "Mau_nhap_lieu_Routing.xlsx";
            }

            var saveFileDialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = fileName };
            if (saveFileDialog.ShowDialog() == true)
            {
                if (SelectedDataType == "Vật tư") await _fileService.GenerateImportTemplateAsync(sampleData as List<MaterialImportExportModel>, saveFileDialog.FileName, "Vật tư", headers);
                else if (SelectedDataType == "Định mức (BOM)") await _fileService.GenerateImportTemplateAsync(sampleData as List<BomImportExportModel>, saveFileDialog.FileName, "BOM", headers);
                else await _fileService.GenerateImportTemplateAsync(sampleData as List<RoutingImportExportModel>, saveFileDialog.FileName, "Routing", headers);

                _notificationService.ShowSuccess("Tải file mẫu thành công!");
            }
        }
        catch (Exception ex) { _notificationService.ShowError($"Lỗi: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task Process()
    {
        if (IsBusy) return;
        
        try 
        {
            IsBusy = true;

            if (IsImportMode)
            {
                await HandleImport();
            }
            else
            {
                await HandleExport();
            }
        }
        catch (Exception ex) { _notificationService.ShowError($"Lỗi: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    private async Task HandleImport()
    {
        if (string.IsNullOrEmpty(SelectedFilePath) || SelectedFilePath == "Chưa chọn tệp...")
        {
            _notificationService.ShowError("Vui lòng chọn file Excel.");
            return;
        }

        if (SelectedDataType == "Vật tư")
        {
            var importedData = await _fileService.ImportFromExcelAsync<MaterialImportExportModel>(SelectedFilePath);
            if (importedData == null || !importedData.Any()) { _notificationService.ShowError("File không có dữ liệu."); return; }

            int success = 0, update = 0, skipped = 0;
            foreach (var item in importedData)
            {
                if (string.IsNullOrWhiteSpace(item.MaterialCode) || string.IsNullOrWhiteSpace(item.MaterialName)) { skipped++; continue; }
                
                string category = "Nguyên liệu";
                if (!string.IsNullOrWhiteSpace(item.Category))
                {
                    var catLower = item.Category.ToLower();
                    if (catLower.Contains("thành phẩm") && !catLower.Contains("bán")) category = "Thành phẩm";
                    else if (catLower.Contains("bán thành phẩm")) category = "Bán thành phẩm";
                }

                var existing = await _masterDataService.GetMaterialByCodeAsync(item.MaterialCode);
                if (existing != null)
                {
                    if (IsUpdateMode)
                    {
                        existing.MaterialName = item.MaterialName; 
                        existing.Category = category;
                        existing.Unit = item.Unit ?? existing.Unit; 
                        existing.MinStock = (int)item.MinStock;
                        existing.UnitPrice = item.UnitPrice;
                        existing.Status = !string.IsNullOrWhiteSpace(item.Status) ? item.Status : existing.Status;

                        if (await _masterDataService.UpdateMaterialAsync(existing)) update++;
                    }
                    else skipped++;
                }
                else
                {
                    var newMat = new Material 
                    { 
                        MaterialCode = item.MaterialCode, 
                        MaterialName = item.MaterialName, 
                        Category = category, 
                        Unit = item.Unit ?? "Cái", 
                        MinStock = (int)item.MinStock, 
                        UnitPrice = item.UnitPrice,
                        Status = !string.IsNullOrWhiteSpace(item.Status) ? item.Status : "Đang sử dụng",
                        CreatedAt = DateTime.Now
                    };
                    if (await _masterDataService.AddMaterialAsync(newMat)) success++;
                }
            }
            _notificationService.ShowSuccess($"Nhập dữ liệu thành công! (Thêm mới: {success}, Cập nhật: {update}, Bỏ qua: {skipped})");
            
            // Log activity
            await _activityService.LogActivityAsync("Nhập file", $"[MasterData] Nhập {SelectedDataType} từ file Excel. Thành công: {success+update}, Thất bại: {skipped}");

            await Back();
        }
        else if (SelectedDataType == "Định mức (BOM)")
        {
            var importedData = await _fileService.ImportFromExcelAsync<BomImportExportModel>(SelectedFilePath);
            if (importedData == null || !importedData.Any()) { _notificationService.ShowError("File không có dữ liệu."); return; }

            int success = 0, skipped = 0;
            foreach (var item in importedData)
            {
                if (string.IsNullOrWhiteSpace(item.ParentCode) || string.IsNullOrWhiteSpace(item.ChildCode)) { skipped++; continue; }

                var parent = await _masterDataService.GetMaterialByCodeAsync(item.ParentCode);
                var child = await _masterDataService.GetMaterialByCodeAsync(item.ChildCode);

                if (parent != null && child != null)
                {
                    var bom = new Bom { ParentId = parent.MaterialId, ChildId = child.MaterialId, QuantityPerUnit = item.BomQuantity };
                    if (await _masterDataService.AddBomItemAsync(bom)) success++;
                }
                else skipped++;
            }
            _notificationService.ShowSuccess($"Nhập BOM xong! Thành công: {success}, Bỏ qua (mã không tồn tại): {skipped}");
            if (success > 0) await Back();
        }
        else // Routing
        {
            var importedData = await _fileService.ImportFromExcelAsync<RoutingImportExportModel>(SelectedFilePath);
            if (importedData == null || !importedData.Any()) { _notificationService.ShowError("File không có dữ liệu."); return; }

            int success = 0, skipped = 0;
            foreach (var item in importedData)
            {
                if (string.IsNullOrWhiteSpace(item.ProductCode) || string.IsNullOrWhiteSpace(item.StepName)) { skipped++; continue; }

                var product = await _masterDataService.GetMaterialByCodeAsync(item.ProductCode);
                if (product != null)
                {
                    var routing = new Routing { ProductId = product.MaterialId, StepNumber = item.StepNumber, StepName = item.StepName, WorkCenter = item.WorkCenter, EstimatedTime = item.EstimatedTime, OutputDescription = item.OutputDescription };
                    if (await _masterDataService.AddRoutingStepAsync(routing)) success++;
                }
                else skipped++;
            }
            _notificationService.ShowSuccess($"Nhập Quy trình xong! Thành công: {success}, Bỏ qua (mã không tồn tại): {skipped}");
            if (success > 0) await Back();
        }
    }

    private async Task HandleExport()
    {
        if (!ExportColumns.Any(c => c.IsSelected)) { _notificationService.ShowError("Vui lòng chọn ít nhất một cột để xuất."); return; }

        object exportData = null;
        string fileNamePrefix = "";

        if (SelectedDataType == "Vật tư")
        {
            var materials = await _masterDataService.GetAllMaterialsAsync();
            var filtered = materials.Where(m => 
            {
                bool statusMatch = false;
                if (SelectedStatus == "Tất cả trạng thái") statusMatch = true;
                else if (SelectedStatus == "Sắp hết")
                {
                    var currentStock = (double)(m.Inventories?.Sum(i => i.CurrentStock ?? 0) ?? 0);
                    var minStock = m.MinStock ?? 0;
                    statusMatch = minStock > 0 && currentStock > 0 && currentStock <= minStock;
                }
                else if (SelectedStatus == "Hết hàng")
                {
                    var currentStock = (double)(m.Inventories?.Sum(i => i.CurrentStock ?? 0) ?? 0);
                    statusMatch = currentStock <= 0;
                }
                else statusMatch = (m.Status == SelectedStatus);
                return statusMatch && (m.CreatedAt == null || (m.CreatedAt >= StartDate && m.CreatedAt <= EndDate.AddDays(1)));
            }).ToList();

            exportData = filtered.Select(m => new MaterialImportExportModel
            {
                MaterialCode = m.MaterialCode, MaterialName = m.MaterialName, Category = m.Category,
                Unit = m.Unit, MinStock = m.MinStock ?? 0, UnitPrice = m.UnitPrice ?? 0, Status = m.Status
            }).ToList();
            fileNamePrefix = "Danh_muc_Vat_tu";
        }
        else if (SelectedDataType == "Định mức (BOM)")
        {
            var boms = await _masterDataService.GetAllBomsAsync();
            exportData = boms.Select(b => new BomImportExportModel
            {
                ParentCode = b.Parent?.MaterialCode, ParentName = b.Parent?.MaterialName,
                ChildCode = b.Child?.MaterialCode, ChildName = b.Child?.MaterialName,
                BomQuantity = b.QuantityPerUnit, Unit = b.Child?.Unit
            }).ToList();
            fileNamePrefix = "Dinh_muc_BOM";
        }
        else // Routing
        {
            var routings = await _masterDataService.GetAllRoutingsAsync();
            exportData = routings.Select(r => new RoutingImportExportModel
            {
                ProductCode = r.Product?.MaterialCode, ProductName = r.Product?.MaterialName,
                StepNumber = r.StepNumber, StepName = r.StepName,
                WorkCenter = r.WorkCenter, EstimatedTime = r.EstimatedTime,
                OutputDescription = r.OutputDescription
            }).ToList();
            fileNamePrefix = "Quy_trinh_Routing";
        }

        if (exportData == null || !(exportData as System.Collections.IEnumerable).Cast<object>().Any()) 
        { _notificationService.ShowError("Không tìm thấy dữ liệu phù hợp với bộ lọc."); return; }

        string ext = IsPdfFormat ? "pdf" : "xlsx";
        string filter = IsPdfFormat ? "PDF Files (*.pdf)|*.pdf" : "Excel Workbook (*.xlsx)|*.xlsx";

        var saveFileDialog = new SaveFileDialog { Filter = filter, FileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd}.{ext}" };
        if (saveFileDialog.ShowDialog() == true)
        {
            var selectedCols = ExportColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            bool ok;
            if (IsPdfFormat)
            {
                // Dynamic invoke or specific call for PDF
                if (SelectedDataType == "Vật tư") ok = await _fileService.ExportToPdfAsync(exportData as List<MaterialImportExportModel>, saveFileDialog.FileName, PageTitle.ToUpper(), selectedCols);
                else if (SelectedDataType == "Định mức (BOM)") ok = await _fileService.ExportToPdfAsync(exportData as List<BomImportExportModel>, saveFileDialog.FileName, PageTitle.ToUpper(), selectedCols);
                else ok = await _fileService.ExportToPdfAsync(exportData as List<RoutingImportExportModel>, saveFileDialog.FileName, PageTitle.ToUpper(), selectedCols);
            }
            else
            {
                if (SelectedDataType == "Vật tư") ok = await _fileService.ExportToExcelAsync(exportData as List<MaterialImportExportModel>, saveFileDialog.FileName, "Vật tư", selectedCols, PageTitle.ToUpper());
                else if (SelectedDataType == "Định mức (BOM)") ok = await _fileService.ExportToExcelAsync(exportData as List<BomImportExportModel>, saveFileDialog.FileName, "BOM", selectedCols, PageTitle.ToUpper());
                else ok = await _fileService.ExportToExcelAsync(exportData as List<RoutingImportExportModel>, saveFileDialog.FileName, "Routing", selectedCols, PageTitle.ToUpper());
            }

            if (ok) _notificationService.ShowSuccess($"Xuất file {ext.ToUpper()} thành công!");
        }
    }
}
