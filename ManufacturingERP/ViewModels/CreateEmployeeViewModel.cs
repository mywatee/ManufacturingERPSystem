using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.Core;

namespace ManufacturingERP.ViewModels;

public partial class CreateEmployeeViewModel : ViewModelBase
{
    private readonly IUserManagementService _userService;
    private readonly IHRService _hrService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string _employeeCode = string.Empty;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _selectedDepartment = "Sản xuất";
    [ObservableProperty] private string _position = "Công nhân";
    [ObservableProperty] private decimal _basicSalary = 6000000;
    [ObservableProperty] private decimal _piecesRate = 15000;
    [ObservableProperty] private int _productivityThreshold = 500;
    [ObservableProperty] private DateTime _joinDate = DateTime.Today;

    public List<string> Departments { get; } = new() { "Sản xuất", "Kho bãi", "Kỹ thuật", "Hành chính", "Kế toán" };

    public CreateEmployeeViewModel(
        IUserManagementService userService, 
        IHRService hrService, 
        INavigationService navigationService,
        INotificationService notificationService)
    {
        _userService = userService;
        _hrService = hrService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        
        _ = GenerateCodeAsync();
    }

    [RelayCommand]
    private async Task GenerateCodeAsync()
    {
        EmployeeCode = await _hrService.GetNextEmployeeCodeAsync();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            _notificationService.ShowError("Họ và tên không được để trống.");
            return;
        }

        IsBusy = true;
        try 
        {
            // 1. Create Employee
            var employee = new Employee
            {
                EmployeeCode = string.IsNullOrWhiteSpace(EmployeeCode) ? await _hrService.GetNextEmployeeCodeAsync() : EmployeeCode,
                FullName = FullName,
                Email = Email,
                Phone = Phone,
                Department = SelectedDepartment,
                Position = Position,
                BasicSalary = BasicSalary,
                PiecesRate = PiecesRate,
                ProductivityThreshold = ProductivityThreshold,
                JoinDate = JoinDate,
                Status = "Đang làm việc"
            };

            var createdEmployee = await _hrService.AddEmployeeAsync(employee);

            // 2. Create User Account (linked to employee)
            string username = FullName.Split(' ').Last().ToLower() + new Random().Next(100, 999);
            var newUser = new User
            {
                Username = username,
                EmployeeId = createdEmployee.EmployeeId,
                IsActive = true
            };

            try
            {
                await _userService.CreateUserAsync(newUser, "Nhân viên vận hành", "Staff@123");
            }
            catch
            {
                await _hrService.DeleteEmployeeAsync(createdEmployee.EmployeeId);
                throw;
            }
            
            _notificationService.ShowSuccess($"Đã tạo nhân viên {FullName} thành công. Tài khoản: {username}");
            GoBack();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi lưu nhân viên: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        var hrVm = _navigationService.NavigateTo<HRViewModel>();
        await hrVm.LoadDataAsync();
    }
}
