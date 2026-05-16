using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ManufacturingERP.Models;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels
{
    public partial class AdminViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IAuthService _authService;
        private readonly INotificationService _notificationService;
        private readonly IUserManagementService _userManagementService;
        private readonly IPermissionService _permissionService;
        private readonly IAuditLogService _auditLogService;

        [ObservableProperty]
        private string _activeTab = "users";

        // Business Profile
        [ObservableProperty] private string _companyName = "Công ty TNHH Sản xuất ABC";
        [ObservableProperty] private string _taxCode = "0123456789";
        [ObservableProperty] private string _address = "123 Đường ABC, Quận XYZ, TP. Hồ Chí Minh";
        [ObservableProperty] private string _phone = "028 1234 5678";
        [ObservableProperty] private string _email = "info@company.vn";

        // Format Configuration
        [ObservableProperty] private string _selectedDateFormat = "DD/MM/YYYY";
        [ObservableProperty] private ObservableCollection<string> _availableDateFormats = new() { "DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD" };
        
        [ObservableProperty] private string _selectedTimeFormat = "24 giờ (14:30)";
        [ObservableProperty] private ObservableCollection<string> _availableTimeFormats = new() { "24 giờ (14:30)", "12 giờ (02:30 PM)" };
        
        [ObservableProperty] private string _selectedCurrency = "VND (đ)";
        [ObservableProperty] private ObservableCollection<string> _availableCurrencies = new() { "VND (đ)", "USD ($)", "EUR (€)" };
        
        [ObservableProperty] private string _selectedTimeZone = "GMT+7 (Hà Nội, TP.HCM)";
        [ObservableProperty] private ObservableCollection<string> _availableTimeZones = new() { "GMT+7 (Hà Nội, TP.HCM)", "GMT+8 (Singapore, Beijing)", "GMT+0 (London)" };

        // Security Settings (Bound to UI)
        [ObservableProperty] private int _minPasswordLength = 8;
        [ObservableProperty] private string _selectedHashAlgorithm = "bcrypt (Khuyến nghị)";
        [ObservableProperty] private ObservableCollection<string> _availableHashAlgorithms = new() { "bcrypt (Khuyến nghị)", "Argon2", "SHA-256" };
        
        [ObservableProperty] private int _sessionTimeout = 30;
        [ObservableProperty] private int _maxLoginAttempts = 5;
        
        [ObservableProperty] private bool _isComplexityRequired = true;
        [ObservableProperty] private bool _isRotationRequired = true;
        [ObservableProperty] private bool _is2FARequired = false;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private ObservableCollection<ModulePermission> _permissions = new();

        [ObservableProperty]
        private ObservableCollection<AuditLogViewModel> _auditLogs = new();

        [ObservableProperty] private int _currentLogPage = 1;
        [ObservableProperty] private int _totalLogPages = 1;
        [ObservableProperty] private string _logSearchTerm = string.Empty;
        [ObservableProperty] private DateTime? _logStartDate;
        [ObservableProperty] private DateTime? _logEndDate;
        private const int LogPageSize = 20;

        [ObservableProperty] private int _currentUserPage = 1;
        [ObservableProperty] private int _totalUserPages = 1;
        [ObservableProperty] private string _userSearchTerm = string.Empty;
        private const int UserPageSize = 20;

        [ObservableProperty]
        private ObservableCollection<PasswordResetRequest> _resetRequests = new();

        [ObservableProperty]
        private int _pendingRequestsCount;

        [ObservableProperty]
        private string _selectedRole = "Admin";

        [ObservableProperty]
        private ObservableCollection<string> _availableRoles = new();

        public partial class ModulePermission : ObservableObject
        {
            public string ModuleKey { get; set; } = string.Empty;
            public string ModuleName { get; set; } = string.Empty;
            [ObservableProperty] private bool _canView;
            [ObservableProperty] private bool _canAdd;
            [ObservableProperty] private bool _canEdit;
            [ObservableProperty] private bool _canDelete;
        }

        public partial class AuditLogViewModel : ObservableObject
        {
            public DateTime Timestamp { get; set; }
            public string UserFullName { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string TableName { get; set; } = string.Empty;
            public string OldValue { get; set; } = string.Empty;
            public string NewValue { get; set; } = string.Empty;
        }

        public AdminViewModel(
            ISettingsService settingsService,
            IAuthService authService,
            INotificationService notificationService,
            IUserManagementService userManagementService,
            IPermissionService permissionService,
            IAuditLogService auditLogService)
        {
            _settingsService = settingsService;
            _authService = authService;
            _notificationService = notificationService;
            _userManagementService = userManagementService;
            _permissionService = permissionService;
            _auditLogService = auditLogService;
             
            LoadUsersAsync();
            _ = LoadRolesAndPermissionsAsync();
            _ = LoadAuditLogsAsync();
            LoadSecuritySettings();
            _ = LoadResetRequestsAsync();
        }

        private async void LoadSecuritySettings()
        {
            MinPasswordLength = await _settingsService.GetSettingIntAsync("MinPasswordLength", 8);
            SelectedHashAlgorithm = await _settingsService.GetSettingAsync("HashAlgorithm", "bcrypt (Khuyến nghị)");
            SessionTimeout = await _settingsService.GetSettingIntAsync("SessionTimeout", 30);
            MaxLoginAttempts = await _settingsService.GetSettingIntAsync("MaxLoginAttempts", 5);
            IsComplexityRequired = await _settingsService.GetSettingBoolAsync("IsComplexityRequired", true);
            IsRotationRequired = await _settingsService.GetSettingBoolAsync("IsRotationRequired", true);
            Is2FARequired = await _settingsService.GetSettingBoolAsync("Is2FARequired", false);
        }

        [RelayCommand]
        private async Task SaveSecuritySettings()
        {
            await _settingsService.SetSettingAsync("MinPasswordLength", MinPasswordLength);
            await _settingsService.SetSettingAsync("HashAlgorithm", SelectedHashAlgorithm);
            await _settingsService.SetSettingAsync("SessionTimeout", SessionTimeout);
            await _settingsService.SetSettingAsync("MaxLoginAttempts", MaxLoginAttempts);
            await _settingsService.SetSettingAsync("IsComplexityRequired", IsComplexityRequired);
            await _settingsService.SetSettingAsync("IsRotationRequired", IsRotationRequired);
            await _settingsService.SetSettingAsync("Is2FARequired", Is2FARequired);
            
            // Add a log entry
            var actor = _authService.CurrentUser;
            AuditLogs.Insert(0, new AuditLogViewModel 
            { 
                Timestamp = DateTime.Now, 
                UserFullName = actor?.Employee?.FullName ?? actor?.Username ?? "Quản trị viên", 
                Action = "Sửa", 
                TableName = "SystemSettings", 
                OldValue = "Cấu hình cũ", 
                NewValue = "Cập nhật chính sách bảo mật" 
            });

            await _auditLogService.LogAsync(actor?.UserId, "Sửa", "SystemSettings", "Cấu hình cũ", "Cập nhật chính sách bảo mật");
        }

        [RelayCommand]
        private void ChangeTab(string tabName) => ActiveTab = tabName;

        [RelayCommand]
        private async Task ToggleUserStatus(User user)
        {
            if (user == null) return;

            string action = (user.IsActive ?? false) ? "KHÓA" : "MỞ KHÓA";
            bool confirm = _notificationService.Confirm(
                $"Bạn có chắc chắn muốn {action} tài khoản của {user.Employee?.FullName ?? user.Username} ({user.Username}) không?", 
                "Xác nhận thay đổi");

            if (!confirm) return;

            user.IsActive = !(user.IsActive ?? false);
            
            try 
            {
                var success = await _userManagementService.ToggleActiveAsync(user.UserId, user.IsActive == true);
                if (!success)
                {
                    _notificationService.ShowError("Không tìm thấy người dùng để cập nhật.");
                    return;
                }

                _notificationService.ShowSuccess($"Đã {(user.IsActive == true ? "mở khóa" : "khóa")} tài khoản {user.Employee?.FullName ?? user.Username} thành công.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Lỗi khi cập nhật trạng thái: {ex.Message}");
                return;
            }

            // Log activity
            var actor = _authService.CurrentUser;
            AuditLogs.Insert(0, new AuditLogViewModel 
            { 
                Timestamp = DateTime.Now, 
                UserFullName = actor?.Employee?.FullName ?? actor?.Username ?? "Admin", 
                Action = (user.IsActive ?? false) ? "Mở khóa" : "Khóa", 
                TableName = "Users", 
                OldValue = $"User: {user.Username}", 
                NewValue = $"IsActive: {user.IsActive}" 
            });

            await _auditLogService.LogAsync(actor?.UserId, (user.IsActive ?? false) ? "Mở khóa" : "Khóa", "Users", $"User: {user.Username}", $"IsActive: {user.IsActive}");
            
            OnPropertyChanged(nameof(Users));
        }

        [RelayCommand]
        private async Task AddUser()
        {
            var dialog = new Views.Dialogs.UserDialog(new User(), AvailableRoles, _notificationService, true);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                try 
                {
                    var tempPassword = PasswordGenerator.GenerateTemporaryPassword();
                    var created = await _userManagementService.CreateUserAsync(dialog.User, dialog.SelectedRole, tempPassword);
                    Users.Add(created);
                    _notificationService.ShowSuccess($"Đã thêm người dùng {created.Employee?.FullName ?? created.Username} thành công. Mật khẩu tạm: {tempPassword}.");

                    var actor = _authService.CurrentUser;
                    await _auditLogService.LogAsync(actor?.UserId, "Thêm", "Users", "-", $"User: {created.Username} | Name: {created.Employee?.FullName}");
                    await LoadAuditLogsAsync();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Lỗi khi thêm người dùng: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task EditUser(User user)
        {
            if (user == null) return;
            
            var dialog = new Views.Dialogs.UserDialog(user, AvailableRoles, _notificationService);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                try 
                {
                    var updated = await _userManagementService.UpdateUserProfileAsync(
                        user.UserId,
                        null, null, null, // We don't update employee info from User dialog anymore or we need a new logic
                        dialog.SelectedRole);

                    if (updated == null)
                    {
                        _notificationService.ShowError("Không tìm thấy người dùng để cập nhật.");
                        return;
                    }

                    _notificationService.ShowSuccess($"Đã cập nhật thông tin cho {user.Employee?.FullName ?? user.Username} thành công.");
                    OnPropertyChanged(nameof(Users));

                    var actor = _authService.CurrentUser;
                    await _auditLogService.LogAsync(actor?.UserId, "Sửa", "Users", $"User: {user.Username}", "Profile updated");
                    await LoadAuditLogsAsync();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Lỗi khi cập nhật người dùng: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteUser(User user)
        {
            if (user == null) return;

            bool confirm = _notificationService.Confirm(
                $"Bạn có chắc chắn muốn XÓA vĩnh viễn người dùng {user.Employee?.FullName ?? user.Username} ({user.Username}) khỏi hệ thống không?", 
                "Xác nhận xóa");

            if (confirm)
            {
                try 
                {
                    await _userManagementService.DeleteUserAsync(user.UserId);
                    Users.Remove(user);
                    _notificationService.ShowSuccess($"Đã xóa người dùng {user.Employee?.FullName ?? user.Username} thành công.");

                    var actor = _authService.CurrentUser;
                    await _auditLogService.LogAsync(actor?.UserId, "Xóa", "Users", $"User: {user.Username} | Name: {user.Employee?.FullName}", "-");
                    await LoadAuditLogsAsync();
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.InnerException?.Message ?? ex.Message;
                    if (errorMessage.Contains("REFERENCE constraint") || errorMessage.Contains("foreign key"))
                    {
                        _notificationService.ShowError($"Không thể xóa {user.Employee?.FullName ?? user.Username} vì người dùng này đã có dữ liệu lịch sử trong hệ thống. Vui lòng sử dụng chức năng 'Khóa tài khoản' thay thế.");
                    }
                    else
                    {
                        _notificationService.ShowError($"Lỗi khi xóa người dùng: {errorMessage}");
                    }
                }
            }
        }

        [RelayCommand]
        private async Task ResetPassword(User user)
        {
            if (user == null) return;

            var dialog = new Views.Dialogs.ResetPasswordDialog(_notificationService);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                try 
                {
                    await _userManagementService.ResetPasswordAsync(user.UserId, dialog.NewPassword);
                    _notificationService.ShowSuccess($"Đã đặt lại mật khẩu cho {user.Employee?.FullName ?? user.Username} thành công.");

                    var actor = _authService.CurrentUser;
                    await _auditLogService.LogAsync(actor?.UserId, "Sửa", "Users", "Password: [HIDDEN]", "Password: Reset to custom password");
                    await LoadAuditLogsAsync();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Lỗi khi reset mật khẩu: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task SelectRole(string roleName)
        {
            SelectedRole = roleName;
            await LoadPermissionsForSelectedRoleAsync();
        }

        [RelayCommand]
        private async Task SavePermissions()
        {
            try
            {
                var dtos = Permissions.Select(p =>
                    new RolePermissionDto(p.ModuleKey, p.ModuleName, p.CanView, p.CanAdd, p.CanEdit, p.CanDelete));

                await _permissionService.SaveRolePermissionsAsync(SelectedRole, dtos);
                _notificationService.ShowSuccess($"Đã lưu phân quyền cho vai trò: {SelectedRole}.");

                var actor = _authService.CurrentUser;
                await _auditLogService.LogAsync(actor?.UserId, "Sửa", "RolePermissions", $"Role: {SelectedRole}", "Permissions updated");
                await LoadAuditLogsAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Lỗi khi lưu phân quyền: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ResetPermissions()
        {
            try
            {
                await _permissionService.ResetRolePermissionsAsync(SelectedRole);
                await LoadPermissionsForSelectedRoleAsync();
                _notificationService.ShowSuccess($"Đã đặt lại phân quyền cho vai trò: {SelectedRole}.");

                var actor = _authService.CurrentUser;
                await _auditLogService.LogAsync(actor?.UserId, "Sửa", "RolePermissions", $"Role: {SelectedRole}", "Permissions reset");
                await LoadAuditLogsAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Lỗi khi đặt lại phân quyền: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ApproveReset(PasswordResetRequest request)
        {
            if (request == null) return;

            var admin = _authService.CurrentUser;
            if (admin == null) return;

            var tempPassword = await _authService.ProcessResetRequestAsync(request.RequestId, true, admin.UserId);
            if (!string.IsNullOrEmpty(tempPassword))
            {
                ResetRequests.Remove(request);
                PendingRequestsCount = ResetRequests.Count;
                
                AuditLogs.Insert(0, new AuditLogViewModel 
                { 
                    Timestamp = DateTime.Now, 
                    UserFullName = admin.Employee?.FullName ?? admin.Username, 
                    Action = "Phê duyệt", 
                    TableName = "ResetRequests", 
                    OldValue = $"User: {request.Username}", 
                    NewValue = "Password Reset Approved" 
                });

                await _auditLogService.LogAsync(admin.UserId, "Phê duyệt", "ResetRequests", $"User: {request.Username}", "Password Reset Approved");
                await LoadAuditLogsAsync();

                System.Windows.MessageBox.Show($"Đã phê duyệt yêu cầu cho {request.Username}. Mật khẩu tạm mới là: {tempPassword}.");
            }
        }

        [RelayCommand]
        private async Task RejectReset(PasswordResetRequest request)
        {
            if (request == null) return;

            var admin = _authService.CurrentUser;
            if (admin == null) return;

            var result = await _authService.ProcessResetRequestAsync(request.RequestId, false, admin.UserId);
            if (result != null)
            {
                ResetRequests.Remove(request);
                PendingRequestsCount = ResetRequests.Count;
                
                AuditLogs.Insert(0, new AuditLogViewModel 
                { 
                    Timestamp = DateTime.Now, 
                    UserFullName = admin.Employee?.FullName ?? admin.Username, 
                    Action = "Từ chối", 
                    TableName = "ResetRequests", 
                    OldValue = $"User: {request.Username}", 
                    NewValue = "Password Reset Rejected" 
                });

                await _auditLogService.LogAsync(admin.UserId, "Từ chối", "ResetRequests", $"User: {request.Username}", "Password Reset Rejected");
                await LoadAuditLogsAsync();
            }
        }

        [RelayCommand]
        private async Task LoadResetRequestsAsync()
        {
            try 
            {
                var requests = await _authService.GetPendingResetRequestsAsync();
                ResetRequests = new ObservableCollection<PasswordResetRequest>(requests);
                PendingRequestsCount = ResetRequests.Count;
            }
            catch { /* Sample data fallback if needed */ }
        }

        private async Task LoadUsersAsync()
        {
            try 
            {
                var result = await _userManagementService.GetPagedUsersAsync(CurrentUserPage, UserPageSize, UserSearchTerm);
                Users = new ObservableCollection<User>(result.Items);
                TotalUserPages = (int)Math.Ceiling(result.TotalCount / (double)UserPageSize);
                if (TotalUserPages == 0) TotalUserPages = 1;
            }
            catch
            {
                LoadSampleUsers();
            }
        }

        [RelayCommand]
        private async Task SearchUsers()
        {
            CurrentUserPage = 1;
            await LoadUsersAsync();
        }

        [RelayCommand]
        private async Task NextUserPage()
        {
            if (CurrentUserPage < TotalUserPages)
            {
                CurrentUserPage++;
                await LoadUsersAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousUserPage()
        {
            if (CurrentUserPage > 1)
            {
                CurrentUserPage--;
                await LoadUsersAsync();
            }
        }

        private void LoadSampleUsers()
        {
            Users.Clear();
            Users.Add(new User { UserId = 1, Username = "huyhoang", Employee = new Employee { FullName = "Nguyễn Huy Hoàng", Email = "huyhoang@company.vn", Phone = "0912345678" }, IsActive = true });
            Users.Add(new User { UserId = 24, Username = "maianh", Employee = new Employee { FullName = "Trần Mai Anh", Email = "maianh@company.vn", Phone = "0923456789" }, IsActive = true });
            Users.Add(new User { UserId = 45, Username = "tuanminh", Employee = new Employee { FullName = "Lê Tuấn Minh", Email = "tuanminh@company.vn", Phone = "0934567890" }, IsActive = true });
            Users.Add(new User { UserId = 89, Username = "thuha", Employee = new Employee { FullName = "Phạm Thu Hà", Email = "thuha@company.vn", Phone = "0945678901" }, IsActive = true });
        }

        private async Task LoadRolesAndPermissionsAsync()
        {
            try
            {
                var roles = await _userManagementService.GetRoleNamesAsync();
                AvailableRoles = new ObservableCollection<string>(roles);
                if (AvailableRoles.Count > 0 && !AvailableRoles.Contains(SelectedRole))
                    SelectedRole = AvailableRoles[0];
            }
            catch
            {
                AvailableRoles = new ObservableCollection<string> { "Admin" };
                if (!AvailableRoles.Contains(SelectedRole)) SelectedRole = "Admin";
            }

            await LoadPermissionsForSelectedRoleAsync();
        }

        private async Task LoadPermissionsForSelectedRoleAsync()
        {
            try
            {
                var dtos = await _permissionService.GetRolePermissionsAsync(SelectedRole);
                Permissions = new ObservableCollection<ModulePermission>(
                    dtos.Select(p => new ModulePermission
                    {
                        ModuleKey = p.ModuleKey,
                        ModuleName = p.ModuleName,
                        CanView = p.CanView,
                        CanAdd = p.CanAdd,
                        CanEdit = p.CanEdit,
                        CanDelete = p.CanDelete
                    }));
            }
            catch
            {
                Permissions = new ObservableCollection<ModulePermission>();
            }
        }

        private async Task LoadAuditLogsAsync()
        {
            try
            {
                var result = await _auditLogService.GetPagedAsync(CurrentLogPage, LogPageSize, LogSearchTerm, LogStartDate, LogEndDate);
                AuditLogs = new ObservableCollection<AuditLogViewModel>(
                    result.Items.Select(l => new AuditLogViewModel
                    {
                        Timestamp = l.Timestamp ?? DateTime.MinValue,
                        UserFullName = l.User?.Employee?.FullName ?? l.User?.Username ?? "N/A",
                        Action = l.Action ?? string.Empty,
                        TableName = l.TableName ?? string.Empty,
                        OldValue = l.OldValue ?? string.Empty,
                        NewValue = l.NewValue ?? string.Empty
                    }));
                
                TotalLogPages = (int)Math.Ceiling(result.TotalCount / (double)LogPageSize);
                if (TotalLogPages == 0) TotalLogPages = 1;
            }
            catch
            {
                AuditLogs = new ObservableCollection<AuditLogViewModel>();
            }
        }

        [RelayCommand]
        private async Task SearchLog()
        {
            CurrentLogPage = 1;
            await LoadAuditLogsAsync();
        }

        [RelayCommand]
        private async Task NextLogPage()
        {
            if (CurrentLogPage < TotalLogPages)
            {
                CurrentLogPage++;
                await LoadAuditLogsAsync();
            }
        }

        [RelayCommand]
        private async Task PreviousLogPage()
        {
            if (CurrentLogPage > 1)
            {
                CurrentLogPage--;
                await LoadAuditLogsAsync();
            }
        }
    }
}
