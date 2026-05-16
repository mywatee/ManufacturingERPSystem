using ManufacturingERP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ManufacturingERP.Views.Dialogs
{
    public partial class UserDialog : Window
    {
        public User User { get; private set; }
        public string SelectedRole { get; private set; }
        private readonly Services.INotificationService _notificationService;

        public UserDialog(User user, IEnumerable<string> roles, Services.INotificationService notificationService, bool isNew = false)
        {
            InitializeComponent();
            _notificationService = notificationService;
            
            if (isNew)
            {
                TitleText.Text = "THÊM NGƯỜI DÙNG MỚI";
                UsernameTextBox.IsEnabled = true;
                User = new User();
            }
            else
            {
                User = new User
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Employee = user.Employee ?? new Employee(),
                    IsActive = user.IsActive
                };
                UsernameTextBox.Text = User.Username;
                UsernameTextBox.IsEnabled = false;
                FullNameTextBox.Text = User.Employee?.FullName;
                EmailTextBox.Text = User.Employee?.Email;
                PhoneTextBox.Text = User.Employee?.Phone;
            }

            RoleComboBox.ItemsSource = roles;
            RoleComboBox.SelectedItem = isNew ? "Nhân viên vận hành" : (user.Roles?.FirstOrDefault()?.RoleName ?? "Nhân viên vận hành");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            List<string> missingFields = new();
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) && UsernameTextBox.IsEnabled) missingFields.Add("Tên đăng nhập");
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text)) missingFields.Add("Họ và tên");
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text)) missingFields.Add("Email");

            if (missingFields.Any())
            {
                _notificationService.ShowWarning($"Vui lòng nhập đầy đủ các thông tin: {string.Join(", ", missingFields)}");
                return;
            }

            // Validation for Email
            string email = EmailTextBox.Text;
            if (!string.IsNullOrWhiteSpace(email) && (!email.Contains("@") || !email.Contains(".")))
            {
                _notificationService.ShowWarning("Định dạng Email không hợp lệ (cần có @ và tên miền).");
                return;
            }

            // Validation for Phone (digits only)
            string phone = PhoneTextBox.Text;
            if (!string.IsNullOrWhiteSpace(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[0-9]+$"))
            {
                _notificationService.ShowWarning("Số điện thoại chỉ được chứa các chữ số.");
                return;
            }

            User.Username = UsernameTextBox.Text;
            if (User.Employee == null) User.Employee = new Employee();
            User.Employee.FullName = FullNameTextBox.Text;
            User.Employee.Email = email;
            User.Employee.Phone = phone;
            SelectedRole = RoleComboBox.SelectedItem?.ToString() ?? "Nhân viên vận hành";
            
            // For new users, we'll set a default password that they should change
            User.PasswordHash = "123456"; // This will be hashed in the ViewModel
            User.IsActive = true;
            
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
