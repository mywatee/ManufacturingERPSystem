using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using ManufacturingERP.Models;

namespace ManufacturingERP.ViewModels
{
    public partial class AdminViewModel : ViewModelBase
    {
        private readonly ManufacturingContext _context;

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

        // Security Settings
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

        [ObservableProperty]
        private string _selectedRole = "Nhân viên vận hành";

        [ObservableProperty]
        private ObservableCollection<string> _availableRoles = new() { "Admin", "Nhân viên vận hành", "QC", "Kế toán" };

        public partial class ModulePermission : ObservableObject
        {
            public string ModuleName { get; set; } = string.Empty;
            [ObservableProperty] private bool _canView;
            [ObservableProperty] private bool _canAdd;
            [ObservableProperty] private bool _canEdit;
            [ObservableProperty] private bool _canDelete;
            [ObservableProperty] private bool _canOverride;
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

        public AdminViewModel(ManufacturingContext context)
        {
            _context = context;
            LoadData();
            LoadPermissions();
            LoadLogs();
        }

        private void LoadData()
        {
            try 
            {
                var dbUsers = _context.Users.Include(u => u.Roles).ToList();
                if (dbUsers.Any())
                {
                    Users = new ObservableCollection<User>(dbUsers);
                }
                else
                {
                    LoadSampleUsers();
                }
            }
            catch
            {
                LoadSampleUsers();
            }
        }

        private void LoadSampleUsers()
        {
            Users.Clear();
            Users.Add(new User { UserId = 1, Username = "huyhoang", FullName = "Nguyễn Huy Hoàng", Email = "huyhoang@company.vn", Phone = "0912345678", IsActive = true });
            Users.Add(new User { UserId = 24, Username = "maianh", FullName = "Trần Mai Anh", Email = "maianh@company.vn", Phone = "0923456789", IsActive = true });
            Users.Add(new User { UserId = 45, Username = "tuanminh", FullName = "Lê Tuấn Minh", Email = "tuanminh@company.vn", Phone = "0934567890", IsActive = true });
            Users.Add(new User { UserId = 89, Username = "thuha", FullName = "Phạm Thu Hà", Email = "thuha@company.vn", Phone = "0945678901", IsActive = true });
            Users.Add(new User { UserId = 156, Username = "vanhung", FullName = "Hoàng Văn Hùng", Email = "vanhung@company.vn", Phone = "0956789012", IsActive = false });
        }

        private void LoadPermissions()
        {
            Permissions.Clear();
            var modules = new[] { "Bảng điều khiển", "Quản trị hệ thống", "Dữ liệu gốc", "Sản xuất", "Kiểm soát chất lượng", "Kho bãi", "Nhân sự & Lương", "Tài chính" };
            foreach (var m in modules)
            {
                Permissions.Add(new ModulePermission { ModuleName = m, CanView = true, CanAdd = (m == "Sản xuất" || m == "Kho bãi"), CanEdit = (m == "Sản xuất") });
            }
        }

        private void LoadLogs()
        {
            AuditLogs.Clear();
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-13 10:30:45"), UserFullName = "Huy Hoàng", Action = "Thêm", TableName = "ProductionOrders", OldValue = "-", NewValue = "LSX-2026-0413 | Số lượng: 500" });
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-13 09:45:22"), UserFullName = "Mai Anh", Action = "Sửa", TableName = "BOM", OldValue = "Định mức: 2.5kg", NewValue = "Định mức: 2.8kg" });
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-13 08:20:15"), UserFullName = "Tuấn Minh", Action = "Xóa", TableName = "WarehouseTransactions", OldValue = "PXK-0412 | Số lượng: 100", NewValue = "-" });
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-13 07:55:33"), UserFullName = "Thu Hà", Action = "Thêm", TableName = "Suppliers", OldValue = "-", NewValue = "NCC-VL-089 | Tên: Công ty TNHH Thép Việt" });
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-13 07:30:10"), UserFullName = "Huy Hoàng", Action = "Sửa", TableName = "Users", OldValue = "Vai trò: Nhân viên", NewValue = "Vai trò: QC" });
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-12 16:45:55"), UserFullName = "Mai Anh", Action = "Thêm", TableName = "QualityControl", OldValue = "-", NewValue = "QC-20260412-045 | Kết quả: Lỗi" });
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-12 15:20:30"), UserFullName = "Văn Hùng", Action = "Sửa", TableName = "ProductionOrders", OldValue = "Trạng thái: Chờ", NewValue = "Trạng thái: Đang làm" });
            AuditLogs.Add(new AuditLogViewModel { Timestamp = DateTime.Parse("2026-04-12 14:10:18"), UserFullName = "Thu Hà", Action = "Xóa", TableName = "WarehouseTransactions", OldValue = "PNK-0411 | Số lượng: 250", NewValue = "-" });
        }

        [RelayCommand]
        private void ChangeTab(string tabName) => ActiveTab = tabName;

        [RelayCommand]
        private void SelectRole(string roleName) => SelectedRole = roleName;
    }
}
