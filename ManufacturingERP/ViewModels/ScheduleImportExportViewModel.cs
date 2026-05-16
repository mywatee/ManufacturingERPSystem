using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.Core;
using Microsoft.Win32;

namespace ManufacturingERP.ViewModels;

public class ScheduleExcelItem
{
    public string? EmployeeCode { get; set; }
    public string? FullName { get; set; }
    public string? WorkDate { get; set; } // Format: dd/MM/yyyy
    public string? ShiftName { get; set; }
    public string? MachineCode { get; set; }
}

public class MultiSelectEmployeeItem : ObservableObject
{
    public Employee Employee { get; set; }
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    public MultiSelectEmployeeItem(Employee e) { Employee = e; }
}

public partial class ScheduleImportExportViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly INotificationService _notificationService;
    private readonly IHRService _hrService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAuthService _authService;
    private readonly IUserManagementService _userService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private int _month = DateTime.Today.Month;
    [ObservableProperty] private int _year = DateTime.Today.Year;
    [ObservableProperty] private string _selectedFilePath = "Chưa chọn tệp...";
    [ObservableProperty] private bool _isExportMode = true;
    [ObservableProperty] private bool _isImportMode = false;
    [ObservableProperty] private bool _isUpdateMode = false;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedDepartment = "Tất cả";
    [ObservableProperty] private bool _isExcelSelected = true;
    [ObservableProperty] private bool _isPdfSelected = false;
    public List<string> Departments { get; } = new() { "Tất cả", "Sản xuất", "Kho bãi", "Kỹ thuật", "Hành chính", "Kế toán" };

    public ObservableCollection<MultiSelectEmployeeItem> EmployeeSelectionList { get; } = new();
    private List<MultiSelectEmployeeItem> _allEmployeesRaw = new();

    public int SelectedEmployeesCount => EmployeeSelectionList.Count(x => x.IsSelected);

    public List<int> AvailableMonths { get; } = Enumerable.Range(1, 12).ToList();
    public List<int> AvailableYears { get; } = Enumerable.Range(2024, 5).ToList();
    public ObservableCollection<ColumnSelectionItem> ExportColumns { get; } = new();
    public ObservableCollection<InstructionItem> Instructions { get; } = new();

    public string PageTitle => IsExportMode ? "Xuất báo cáo Lịch làm việc" : "Nhập dữ liệu Lịch làm việc từ Excel";
    public string ActionButtonText => IsExportMode ? "Bắt đầu xuất file" : "Bắt đầu nạp dữ liệu";

    public ScheduleImportExportViewModel(
        IFileService fileService,
        INotificationService notificationService,
        IHRService hrService,
        IAuditLogService auditLogService,
        IAuthService authService,
        IUserManagementService userService,
        INavigationService navigationService)
    {
        _fileService = fileService;
        _notificationService = notificationService;
        _hrService = hrService;
        _auditLogService = auditLogService;
        _authService = authService;
        _userService = userService;
        _navigationService = navigationService;

        LoadDefaultColumns();
        LoadInstructions();
        _ = LoadEmployeesAsync();
    }

    private async Task LoadEmployeesAsync()
    {
        var employees = await _hrService.GetEmployeesAsync();
        _allEmployeesRaw = employees.Select(e => new MultiSelectEmployeeItem(e)).ToList();
        
        foreach (var item in _allEmployeesRaw)
        {
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(MultiSelectEmployeeItem.IsSelected)) OnPropertyChanged(nameof(SelectedEmployeesCount)); };
        }
        
        ApplyEmployeeFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyEmployeeFilter();
    partial void OnSelectedDepartmentChanged(string value) => ApplyEmployeeFilter();

    private void ApplyEmployeeFilter()
    {
        var filtered = _allEmployeesRaw.AsEnumerable();
        if (SelectedDepartment != "Tất cả")
            filtered = filtered.Where(x => x.Employee.Department == SelectedDepartment);
        
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(x => x.Employee.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                          x.Employee.EmployeeCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        EmployeeSelectionList.Clear();
        foreach (var item in filtered) EmployeeSelectionList.Add(item);
    }

    private void LoadInstructions()
    {
        Instructions.Clear();
        if (IsExportMode)
        {
            Instructions.Add(new InstructionItem("Định dạng báo cáo", "Excel phù hợp để tính toán, PDF phù hợp để in ấn và lưu trữ hồ sơ cứng."));
            Instructions.Add(new InstructionItem("Bộ lọc nâng cao", "Bạn có thể lọc theo Phòng ban hoặc tìm tên nhân viên cụ thể để giới hạn dữ liệu xuất."));
            Instructions.Add(new InstructionItem("Bộ lọc thời gian", "Dữ liệu được trích xuất theo Tháng và Năm bạn đã chọn."));
        }
        else
        {
            Instructions.Add(new InstructionItem("Cấu trúc File", "Các cột bắt buộc: Mã nhân viên, Ngày trực (dd/MM/yyyy), Ca làm việc."));
            Instructions.Add(new InstructionItem("Xử lý trùng lặp", "Chế độ cập nhật sẽ ghi đè lịch mới nếu nhân viên đã có lịch trong ngày đó."));
            Instructions.Add(new InstructionItem("Tên ca trực", "Tên ca trong Excel phải khớp hoàn toàn với tên ca đã cấu hình trong hệ thống."));
        }
    }

    private void LoadDefaultColumns()
    {
        ExportColumns.Clear();
        ExportColumns.Add(new ColumnSelectionItem("Mã nhân viên", true));
        ExportColumns.Add(new ColumnSelectionItem("Họ và Tên", true));
        ExportColumns.Add(new ColumnSelectionItem("Ngày trực", true));
        ExportColumns.Add(new ColumnSelectionItem("Ca làm việc", true));
        ExportColumns.Add(new ColumnSelectionItem("Chuyền sản xuất", true));
    }

    partial void OnIsExportModeChanged(bool value)
    {
        IsImportMode = !value;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(ActionButtonText));
        LoadInstructions();
    }

    [RelayCommand]
    private void Back()
    {
        var hrVm = _navigationService.NavigateTo<HRViewModel>();
        hrVm.IsSchedulesTabActive = true;
        hrVm.IsProfilesTabActive = false;
    }

    [RelayCommand]
    private void SelectFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            Title = "Chọn file Excel lịch trực"
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
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = "Mau_Nhap_Lich_Lam_Viec"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var headers = new Dictionary<string, string>
                {
                    { "EmployeeCode", "Mã nhân viên" },
                    { "FullName", "Họ và Tên" },
                    { "WorkDate", "Ngày trực" },
                    { "ShiftName", "Ca làm việc" },
                    { "MachineCode", "Chuyền sản xuất" }
                };

                // Generate a few empty rows as examples
                var dummyData = new List<ScheduleExcelItem>
                {
                    new() { EmployeeCode = "NV001", FullName = "Nguyễn Văn A", WorkDate = DateTime.Today.ToString("dd/MM/yyyy"), ShiftName = "Ca Hành Chính", MachineCode = "Chuyền 1" }
                };

                bool success = await _fileService.GenerateImportTemplateAsync(dummyData, saveFileDialog.FileName, "Mẫu nhập lịch", headers);
                if (success)
                {
                    _notificationService.ShowSuccess("Tải file mẫu thành công!");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải file mẫu: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task Process()
    {
        if (IsExportMode)
        {
            await ExportAsync();
        }
        else
        {
            await ImportAsync();
        }
    }

    private async Task ExportAsync()
    {
        try
        {
            var start = new DateTime(Year, Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var schedules = await _hrService.GetSchedulesAsync(start, end);

            // Apply filters
            var filtered = schedules.AsEnumerable();
            
            var selectedIds = _allEmployeesRaw.Where(x => x.IsSelected).Select(x => x.Employee.EmployeeId).ToList();
            if (selectedIds.Any())
            {
                filtered = filtered.Where(s => s.User?.EmployeeId != null && selectedIds.Contains(s.User.EmployeeId.Value));
            }
            else
            {
                if (SelectedDepartment != "Tất cả")
                    filtered = filtered.Where(s => s.User?.Employee?.Department == SelectedDepartment);
                
                if (!string.IsNullOrWhiteSpace(SearchText))
                    filtered = filtered.Where(s => (s.User?.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) || 
                                                  (s.User?.Employee?.EmployeeCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var filteredList = filtered.ToList();

            if (!filteredList.Any())
            {
                _notificationService.ShowWarning($"Không có dữ liệu lịch trực phù hợp với bộ lọc trong tháng {Month}/{Year}.");
                return;
            }

            var exportData = filteredList.Select(s => new ScheduleExcelItem
            {
                EmployeeCode = s.User?.Employee?.EmployeeCode,
                FullName = s.User?.FullName,
                WorkDate = s.WorkDate?.ToString("dd/MM/yyyy"),
                ShiftName = s.Shift?.ShiftName,
                MachineCode = s.MachineCode
            }).ToList();

            var saveFileDialog = new SaveFileDialog
            {
                Filter = IsExcelSelected ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF Document (*.pdf)|*.pdf",
                FileName = $"Lich_Lam_Viec_{Month}_{Year}_{DateTime.Now:yyyyMMdd_HHmm}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var selectedColumns = ExportColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
                
                bool success = IsExcelSelected
                    ? await _fileService.ExportToExcelAsync(exportData, saveFileDialog.FileName, "Lịch trực", selectedColumns)
                    : await _fileService.ExportToPdfAsync(exportData, saveFileDialog.FileName, "BÁO CÁO LỊCH TRỰC NHÂN VIÊN", selectedColumns);

                if (success)
                {
                    _notificationService.ShowSuccess("Xuất file thành công!");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi xuất file: " + ex.Message);
        }
    }

    private async Task ImportAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath) || SelectedFilePath == "Chưa chọn tệp...")
        {
            _notificationService.ShowError("Vui lòng chọn file Excel.");
            return;
        }

        try
        {
            var imported = await _fileService.ImportFromExcelAsync<ScheduleExcelItem>(SelectedFilePath);
            if (imported == null || !imported.Any())
            {
                _notificationService.ShowError("File không có dữ liệu hoặc không đúng định dạng.");
                return;
            }

            var shifts = await _hrService.GetShiftsAsync();
            var users = await _userService.GetUsersAsync(); 

            int successCount = 0;
            int errorCount = 0;

            foreach (var item in imported)
            {
                if (string.IsNullOrEmpty(item.EmployeeCode) || string.IsNullOrEmpty(item.WorkDate)) continue;

                var user = users.FirstOrDefault(u => u.Username == item.EmployeeCode || u.Employee?.EmployeeCode == item.EmployeeCode);
                var shift = shifts.FirstOrDefault(s => s.ShiftName != null && s.ShiftName.Contains(item.ShiftName ?? "", StringComparison.OrdinalIgnoreCase));

                if (user != null && shift != null && DateTime.TryParseExact(item.WorkDate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    var schedule = new EmployeeSchedule
                    {
                        UserId = user.UserId,
                        ShiftId = shift.ShiftId,
                        WorkDate = DateOnly.FromDateTime(date),
                        MachineCode = item.MachineCode
                    };

                    bool success = await _hrService.AssignScheduleAsync(schedule);
                    if (success) successCount++;
                    else errorCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            _notificationService.ShowSuccess($"Đã xử lý xong. Thành công: {successCount}, Thất bại/Bỏ qua: {errorCount}");
            
            // Log
            var currentUser = _authService.CurrentUser;
            await _auditLogService.LogAsync(currentUser?.UserId, "Nhập lịch Excel", "EmployeeSchedules", "-", $"Thành công: {successCount}, Thất bại: {errorCount}");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi nhập file: " + ex.Message);
        }
    }
}
