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

public partial class ImportExportViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IProductionService _productionService;
    private readonly INavigationService _navigationService;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly IActivityService _activityService;
    private readonly IAccessControlService _accessControlService;

    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;
    [ObservableProperty] private string _selectedStatus = "Tất cả trạng thái";
    [ObservableProperty] private string _selectedFilePath = "Chưa chọn tệp...";
    [ObservableProperty] private bool _isExportMode = true;
    [ObservableProperty] private bool _isImportMode = false;
    [ObservableProperty] private bool _isExcelSelected = true;
    [ObservableProperty] private bool _isPdfSelected = false;
    [ObservableProperty] private bool _isUpdateMode = false;

    public string PageTitle => IsExportMode ? "Xuất báo cáo Lệnh sản xuất" : "Nhập dữ liệu Lệnh sản xuất";
    public string ActionButtonText => IsExportMode ? "Bắt đầu xuất file" : "Bắt đầu nạp dữ liệu";

    public List<string> Statuses { get; } = new() { "Tất cả trạng thái", "Đang sản xuất", "Chờ", "Hoàn thành", "Tạm dừng" };
    public List<InstructionItem> Instructions { get; } = new();
    public List<ColumnSelectionItem> ExportColumns { get; } = new();

    public ImportExportViewModel(
        IProductionService productionService, 
        INavigationService navigationService, 
        IFileService fileService,
        INotificationService notificationService,
        DashboardViewModel dashboardViewModel,
        IActivityService activityService,
        IAccessControlService accessControlService)
    {
        _productionService = productionService;
        _navigationService = navigationService;
        _fileService = fileService;
        _notificationService = notificationService;
        _dashboardViewModel = dashboardViewModel;
        _activityService = activityService;
        _accessControlService = accessControlService;
        
        SelectedStatus = Statuses[0];
        LoadInstructions();
        LoadDefaultColumns();
    }

    private void LoadDefaultColumns()
    {
        ExportColumns.Clear();
        ExportColumns.Add(new ColumnSelectionItem("Mã lệnh sản xuất", true));
        ExportColumns.Add(new ColumnSelectionItem("Tên sản phẩm", true));
        ExportColumns.Add(new ColumnSelectionItem("Số lượng", true));
        ExportColumns.Add(new ColumnSelectionItem("Tiến độ thực tế", true));
        ExportColumns.Add(new ColumnSelectionItem("Trạng thái hiện tại", true));
        ExportColumns.Add(new ColumnSelectionItem("Hạn hoàn thành", true));
        ExportColumns.Add(new ColumnSelectionItem("Thời gian tạo", true));
        ExportColumns.Add(new ColumnSelectionItem("Người tạo", true));
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
        if (IsExportMode)
        {
            Instructions.Add(new InstructionItem("Định dạng báo cáo", "Excel phù hợp để tính toán, PDF phù hợp để in ấn và lưu trữ hồ sơ cứng."));
            Instructions.Add(new InstructionItem("Bộ lọc thời gian", "Chỉ dữ liệu trong khoảng thời gian đã chọn mới được trích xuất."));
            Instructions.Add(new InstructionItem("Phân quyền", "Báo cáo có thể chứa thông tin bảo mật, vui lòng lưu trữ cẩn thận."));
        }
        else
        {
            Instructions.Add(new InstructionItem("Cấu trúc File", "Các cột bắt buộc: Mã lệnh sản xuất, Tên sản phẩm, Số lượng mục tiêu, Hạn hoàn thành."));
            Instructions.Add(new InstructionItem("Xử lý trùng lặp", "Mã lệnh sản xuất là duy nhất. Chế độ cập nhật sẽ ghi đè lên lệnh cũ."));
            Instructions.Add(new InstructionItem("Kiểm tra dữ liệu", "Hệ thống sẽ bỏ qua các dòng thiếu thông tin quan trọng."));
        }
    }

    [RelayCommand]
    private async Task Back() 
    {
        var vm = _navigationService.NavigateTo<ProductionViewModel>();
        await vm.LoadDataAsync();
    }

    [RelayCommand]
    private void SelectFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            Title = "Chọn file Excel nguồn để nhập dữ liệu"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            SelectedFilePath = openFileDialog.FileName;
        }
    }

    [RelayCommand]
    private async Task Process()
    {
        try 
        {
            var requiredAction = IsExportMode
                ? PermissionAction.View
                : (IsUpdateMode ? PermissionAction.Edit : PermissionAction.Add);

            if (!await _accessControlService.HasAsync(SystemModules.Production, requiredAction))
            {
                var actionLabel = requiredAction == PermissionAction.View ? "Xem" :
                    requiredAction == PermissionAction.Edit ? "Sửa" : "Thêm";
                _notificationService.ShowError($"Bạn không có quyền {actionLabel} trong phân hệ Sản xuất.");
                return;
            }

            if (IsExportMode)
            {
                // 1. Validate Status
                if (string.IsNullOrWhiteSpace(SelectedStatus))
                {
                    _notificationService.ShowError("Vui lòng chọn trạng thái lệnh sản xuất.");
                    return;
                }

                // 2. Validate Date Range
                if (EndDate < StartDate)
                {
                    _notificationService.ShowError("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
                    return;
                }

                // 3. Validate Format
                if (!IsExcelSelected && !IsPdfSelected)
                {
                    _notificationService.ShowError("Vui lòng chọn định dạng tệp (Excel hoặc PDF).");
                    return;
                }

                // 4. Validate Columns
                if (!ExportColumns.Any(c => c.IsSelected))
                {
                    _notificationService.ShowError("Vui lòng tích chọn ít nhất một cột dữ liệu để hiển thị.");
                    return;
                }

                var workOrders = await _productionService.GetFilteredWorkOrdersAsync(StartDate, EndDate, SelectedStatus);
                if (workOrders == null || !workOrders.Any())
                {
                    _notificationService.ShowError("Không có dữ liệu phù hợp với bộ lọc đã chọn.");
                    return;
                }

                var exportData = new List<UrgentOrder>();
                foreach (var wo in workOrders)
                {
                    if (wo.WorkOrderItems != null && wo.WorkOrderItems.Any())
                    {
                        foreach (var item in wo.WorkOrderItems)
                        {
                            exportData.Add(new UrgentOrder
                            {
                                Code = wo.Wocode,
                                Product = item.Product?.MaterialName ?? "N/A",
                                Quantity = item.TargetQty,
                                Status = wo.Status,
                                StartDate = wo.StartDate?.ToString("dd/MM/yyyy") ?? "N/A",
                                Deadline = wo.EndDate?.ToString("dd/MM/yyyy") ?? "N/A",
                                CreatedAt = wo.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A",
                                CreatedBy = wo.CreatedByNavigation?.Employee?.FullName ?? wo.CreatedByNavigation?.Username ?? "Hệ thống",
                                Progress = item.TargetQty > 0 ? (int)(item.ActualQty * 100 / item.TargetQty) : 0,
                                IsUrgent = wo.IsUrgent
                            });
                        }
                    }
                    else
                    {
                        // Fallback if no items (shouldn't happen with real data but for safety)
                        exportData.Add(new UrgentOrder
                        {
                            Code = wo.Wocode,
                            Product = "Không có sản phẩm",
                            Status = wo.Status,
                            CreatedAt = wo.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"
                        });
                    }
                }

                var filter = IsExcelSelected ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF Document (*.pdf)|*.pdf";
                var statusSlug = SelectedStatus == "Tất cả trạng thái" ? "Tat_ca" : SelectedStatus.Replace(" ", "_");
                var fileName = $"Bao_cao_LSX_{statusSlug}_{StartDate:yyyyMMdd}_den_{EndDate:yyyyMMdd}_{DateTime.Now:HHmm}";
                var saveFileDialog = new SaveFileDialog { Filter = filter, FileName = fileName };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var selectedColumnNames = ExportColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();

                    bool success = IsExcelSelected 
                        ? await _fileService.ExportToExcelAsync(exportData, saveFileDialog.FileName, "Dữ liệu", selectedColumnNames) 
                        : await _fileService.ExportToPdfAsync(exportData, saveFileDialog.FileName, "BÁO CÁO", selectedColumnNames);

                    if (success)
                    {
                        _notificationService.ShowSuccess("Xuất file thành công!");
                        // Log activity summary
                        await _activityService.LogActivityAsync("Xuất file", $"[Production] Xuất báo cáo Lệnh sản xuất ra {(IsExcelSelected ? "Excel" : "PDF")}.");
                    }
                    else
                    {
                        _notificationService.ShowError("Không thể lưu file. Hãy đóng file Excel/PDF nếu đang mở và thử lại.");
                    }
                }
            }
            else
            {
                // ... (Keep existing import logic)
                if (string.IsNullOrEmpty(SelectedFilePath) || SelectedFilePath == "Chưa chọn tệp...")
                {
                    _notificationService.ShowError("Chọn file trước.");
                    return;
                }

                var importedData = await _fileService.ImportFromExcelAsync<UrgentOrder>(SelectedFilePath);
                if (importedData != null)
                {
                    var allProducts = await _productionService.GetProductsAsync();
                    int count = 0;
                    int skipped = 0;
                    // Group imported data by Code to handle multi-item orders
                    var groupedData = importedData.GroupBy(i => i.Code).ToList();
                    
                    foreach(var group in groupedData) {
                        var wocode = group.Key ?? $"LSX-IMP-{DateTime.Now:yyyyMMdd-HHmm}";
                        
                        // Validate items in the group
                        var validItems = new List<UrgentOrder>();
                        foreach(var item in group) {
                            if (!string.IsNullOrEmpty(item.Product) && item.Quantity > 0) {
                                validItems.Add(item);
                            } else {
                                skipped++;
                            }
                        }

                        if (!validItems.Any()) continue;

                        var firstItem = validItems.First();
                        
                        // Check for existing Work Order
                        WorkOrder? existingWO = await _productionService.GetWorkOrderByCodeAsync(wocode);

                        if (existingWO != null) {
                            if (!IsUpdateMode) {
                                skipped += validItems.Count;
                                continue; 
                            }
                            
                            // Update header info from the first occurrence in the group
                            existingWO.Status = firstItem.Status ?? existingWO.Status;
                            existingWO.IsUrgent = firstItem.IsUrgent;
                            
                            if (!string.IsNullOrEmpty(firstItem.StartDate) && DateTime.TryParse(firstItem.StartDate, out var sd)) existingWO.StartDate = sd;
                            if (!string.IsNullOrEmpty(firstItem.Deadline) && DateTime.TryParse(firstItem.Deadline, out var ed)) existingWO.EndDate = ed;

                            existingWO.WorkOrderItems.Clear();
                            foreach(var item in validItems) {
                                // Smart matching: Trim and check if names match or one contains the other
                                var productName = item.Product?.Trim().ToLower();
                                var product = allProducts.FirstOrDefault(p => 
                                {
                                    var dbName = p.MaterialName?.Trim().ToLower() ?? "";
                                    return dbName == productName || 
                                           dbName.Contains(productName ?? "---") || 
                                           (productName?.Contains(dbName) == true && dbName.Length > 3);
                                });
                                if (product != null) {
                                    existingWO.WorkOrderItems.Add(new WorkOrderItem {
                                        ProductId = product.MaterialId,
                                        TargetQty = item.Quantity,
                                        ActualQty = 0,
                                        Status = "Chờ"
                                    });
                                }
                            }

                            if (await _productionService.UpdateWorkOrderAsync(existingWO))
                            {
                                count++;
                                await _activityService.LogActivityAsync("Cập nhật", $"Cập nhật LSX {wocode}", "Admin");
                            }
                        } else {
                            // Create new Work Order
                            var newWO = new WorkOrder { 
                                Wocode = wocode,
                                Status = firstItem.Status ?? "Chờ",
                                IsUrgent = firstItem.IsUrgent,
                                CreatedAt = DateTime.Now // Ensure it shows up on top of Dashboard
                            };

                            if (!string.IsNullOrEmpty(firstItem.StartDate) && DateTime.TryParse(firstItem.StartDate, out var sd)) newWO.StartDate = sd;
                            if (!string.IsNullOrEmpty(firstItem.Deadline) && DateTime.TryParse(firstItem.Deadline, out var ed)) newWO.EndDate = ed;
                            
                            foreach(var item in validItems) {
                                // Smart matching: Trim and check if names match or one contains the other
                                var productName = item.Product?.Trim().ToLower();
                                var product = allProducts.FirstOrDefault(p => 
                                {
                                    var dbName = p.MaterialName?.Trim().ToLower() ?? "";
                                    return dbName == productName || 
                                           dbName.Contains(productName ?? "---") || 
                                           (productName?.Contains(dbName) == true && dbName.Length > 3);
                                });
                                    
                                if (product != null) {
                                    newWO.WorkOrderItems.Add(new WorkOrderItem {
                                        ProductId = product.MaterialId,
                                        TargetQty = item.Quantity,
                                        ActualQty = 0,
                                        Status = "Chờ"
                                    });
                                }
                            }

                            if (await _productionService.CreateWorkOrderAsync(newWO))
                            {
                                count++;
                                await _activityService.LogActivityAsync("Thêm", $"Nhập mới LSX {wocode}", "Admin");
                            }
                        }
                    }
                    string actionText = IsUpdateMode ? "xử lý" : "nhập mới";
                    string resultMsg = $"Đã {actionText} {count} lệnh sản xuất thành công.";
                    if (skipped > 0) resultMsg += $" (Bỏ qua {skipped} dòng do dữ liệu không hợp lệ hoặc đã tồn tại)";
                    
                    _notificationService.ShowSuccess(resultMsg);
                    
                    // Log activity summary
                    await _activityService.LogActivityAsync("Nhập file", $"[Production] Nhập dữ liệu Lệnh sản xuất từ file Excel. Thành công: {count}, Bỏ qua: {skipped}");

                    _navigationService.NavigateTo<ProductionViewModel>();
                    await _dashboardViewModel.InitializeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi: {ex.Message}");
        }
    }
}

public class ColumnSelectionItem : ObservableObject
{
    public string Name { get; set; }
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    public ColumnSelectionItem(string n, bool s) { Name = n; IsSelected = s; }
}

public class InstructionItem
{
    public string Title { get; set; }
    public string Content { get; set; }
    public InstructionItem(string t, string c) { Title = t; Content = c; }
}
