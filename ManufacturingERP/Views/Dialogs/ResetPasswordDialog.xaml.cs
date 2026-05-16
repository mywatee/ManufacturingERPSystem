using System.Windows;

namespace ManufacturingERP.Views.Dialogs
{
    public partial class ResetPasswordDialog : Window
    {
        public string NewPassword { get; private set; }
        private readonly Services.INotificationService _notificationService;

        public ResetPasswordDialog(Services.INotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            string pass = NewPasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(pass) || pass.Length < 8)
            {
                _notificationService.ShowWarning("Mật khẩu phải có ít nhất 8 ký tự.");
                return;
            }

            // Complexity check: at least 1 uppercase, 1 lowercase, 1 digit
            bool hasUpper = System.Text.RegularExpressions.Regex.IsMatch(pass, "[A-Z]");
            bool hasLower = System.Text.RegularExpressions.Regex.IsMatch(pass, "[a-z]");
            bool hasDigit = System.Text.RegularExpressions.Regex.IsMatch(pass, "[0-9]");

            if (!hasUpper || !hasLower || !hasDigit)
            {
                _notificationService.ShowWarning("Mật khẩu phải bao gồm chữ hoa, chữ thường và số.");
                return;
            }

            if (pass != confirm)
            {
                _notificationService.ShowWarning("Mật khẩu nhập lại không khớp.");
                return;
            }

            NewPassword = pass;
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
