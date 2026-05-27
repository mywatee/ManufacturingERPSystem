using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.Core;

namespace ManufacturingERP.ViewModels;

public partial class EditEmployeeViewModel : ViewModelBase
{
    private readonly IHRService _hrService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private Employee? _employee;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _selectedDepartment = string.Empty;
    [ObservableProperty] private string _position = string.Empty;
    [ObservableProperty] private decimal _basicSalary;
    [ObservableProperty] private decimal _piecesRate;
    [ObservableProperty] private int _productivityThreshold;
    [ObservableProperty] private DateTime _joinDate;
    [ObservableProperty] private string _status = "Đang làm việc";

    public List<string> Departments { get; } = new() { "Sản xuất", "Kho bãi", "Kỹ thuật", "Hành chính", "Kế toán" };
    public List<string> Statuses { get; } = new() { "Đang làm việc", "Nghỉ phép", "Nghỉ việc" };

    public EditEmployeeViewModel(
        IHRService hrService, 
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _hrService = hrService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    // This will be called by the parent ViewModel after navigation
    public void SetEmployee(Employee employee)
    {
        Employee = employee;
        FullName = employee.FullName;
        Email = employee.Email ?? string.Empty;
        Phone = employee.Phone ?? string.Empty;
        SelectedDepartment = employee.Department ?? "Sản xuất";
        Position = employee.Position ?? "Công nhân";
        BasicSalary = employee.BasicSalary ?? 6000000;
        PiecesRate = employee.PiecesRate ?? 15000;
        ProductivityThreshold = employee.ProductivityThreshold ?? 500;
        JoinDate = employee.JoinDate ?? DateTime.Today;
        
        // Map legacy English status to Vietnamese if needed
        string currentStatus = employee.Status ?? "Đang làm việc";
        if (currentStatus == "Active") currentStatus = "Đang làm việc";
        else if (currentStatus == "OnLeave") currentStatus = "Nghỉ phép";
        else if (currentStatus == "Inactive") currentStatus = "Nghỉ việc";
        
        Status = currentStatus;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Employee == null) return;
        if (string.IsNullOrWhiteSpace(FullName))
        {
            _notificationService.ShowError("Họ và tên không được để trống.");
            return;
        }

        IsBusy = true;
        try 
        {
            Employee.FullName = FullName;
            Employee.Email = Email;
            Employee.Phone = Phone;
            Employee.Department = SelectedDepartment;
            Employee.Position = Position;
            Employee.BasicSalary = BasicSalary;
            Employee.PiecesRate = PiecesRate;
            Employee.ProductivityThreshold = ProductivityThreshold;
            Employee.JoinDate = JoinDate;
            Employee.Status = Status;

            await _hrService.UpdateEmployeeAsync(Employee);
            
            _notificationService.ShowSuccess($"Đã cập nhật thông tin nhân viên {FullName} thành công.");
            GoBack();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi cập nhật thông tin: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        var vm = _navigationService.NavigateTo<HRViewModel>();
        await vm.LoadDataAsync();
    }
}
