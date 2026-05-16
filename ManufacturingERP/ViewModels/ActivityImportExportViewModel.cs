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

public partial class ActivityImportExportViewModel : ViewModelBase
{
    public class ActivityExportModel
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public string User { get; set; }
        public string Time { get; set; }
    }
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IActivityService _activityService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private DateTime _startDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;
    [ObservableProperty] private string _selectedType = "Tất cả";
    [ObservableProperty] private bool _isExcelSelected = true;
    [ObservableProperty] private bool _isPdfSelected = false;

    public List<string> ActivityTypes { get; } = new() { "Tất cả", "Thêm", "Sửa", "Xóa", "Hệ thống" };
    public List<InstructionItem> Instructions { get; } = new();
    public List<ColumnSelectionItem> ExportColumns { get; } = new();

    public ActivityImportExportViewModel(
        IActivityService activityService, 
        INavigationService navigationService, 
        IFileService fileService,
        INotificationService notificationService)
    {
        _activityService = activityService;
        _navigationService = navigationService;
        _fileService = fileService;
        _notificationService = notificationService;
        
        LoadInstructions();
        LoadDefaultColumns();
    }

    private void LoadDefaultColumns()
    {
        ExportColumns.Clear();
        ExportColumns.Add(new ColumnSelectionItem("Loại", true));
        ExportColumns.Add(new ColumnSelectionItem("Nội dung chi tiết", true));
        ExportColumns.Add(new ColumnSelectionItem("Người thực hiện", true));
        ExportColumns.Add(new ColumnSelectionItem("Thời gian", true));
    }

    private void LoadInstructions()
    {
        Instructions.Clear();
        Instructions.Add(new InstructionItem("Định dạng báo cáo", "Excel (.xlsx) phù hợp để lưu trữ lâu dài và phân tích số liệu."));
        Instructions.Add(new InstructionItem("Khoảng thời gian", "Hệ thống sẽ trích xuất tất cả nhật ký phát sinh trong khoảng thời gian được chọn."));
        Instructions.Add(new InstructionItem("Độ chính xác", "Báo cáo bao gồm thời gian chính xác đến từng phút và tên người thực hiện."));
    }

    [RelayCommand]
    private void Back() => _navigationService.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private async Task Process()
    {
        if (IsBusy) return;

        if (StartDate > EndDate)
        {
            _notificationService.ShowError("Khoảng thời gian không hợp lệ (Ngày bắt đầu phải nhỏ hơn ngày kết thúc).");
            return;
        }

        try
        {
            IsBusy = true;
            
            // Lấy dữ liệu từ Database thông qua Service (Server-side filtering)
            var logs = await _activityService.GetFilteredActivitiesAsync(StartDate, EndDate, SelectedType);

            if (!logs.Any())
            {
                _notificationService.ShowError("Không có dữ liệu nhật ký phù hợp để xuất.");
                return;
            }

            var exportData = logs.Select(l => new ActivityExportModel
            {
                Type = l.ActivityType,
                Content = l.Content,
                User = l.PerformedBy ?? "Admin",
                Time = l.Timestamp?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"
            }).ToList();

            var filter = IsExcelSelected ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF Document (*.pdf)|*.pdf";
            var typeSlug = SelectedType == "Tất cả" ? "Tat_ca" : SelectedType;
            var fileName = $"Bao_cao_Nhat_ky_{typeSlug}_{StartDate:yyyyMMdd}_den_{EndDate:yyyyMMdd}";
            var saveFileDialog = new SaveFileDialog { Filter = filter, FileName = fileName };

            if (saveFileDialog.ShowDialog() == true)
            {
                var selectedColumns = ExportColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
                bool success = IsExcelSelected 
                    ? await _fileService.ExportToExcelAsync(exportData, saveFileDialog.FileName, "Nhật ký", selectedColumns, "BÁO CÁO NHẬT KÝ HOẠT ĐỘNG") 
                    : await _fileService.ExportToPdfAsync(exportData, saveFileDialog.FileName, "NHẬT KÝ HOẠT ĐỘNG", selectedColumns);

                if (success) _notificationService.ShowSuccess("Xuất báo cáo nhật ký thành công!");
                else _notificationService.ShowError("Không thể lưu file. Vui lòng kiểm tra lại.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"Lỗi xuất file: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
