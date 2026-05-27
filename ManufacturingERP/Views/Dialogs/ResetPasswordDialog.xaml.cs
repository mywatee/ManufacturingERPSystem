using System.Windows;

namespace ManufacturingERP.Views.Dialogs
{
    public partial class ResetPasswordDialog : Window
    {
        public string NewPassword { get; private set; }
        private readonly Services.INotificationService _notificationService;
        private readonly int _minLength;
        private readonly bool _complexityRequired;

        public ResetPasswordDialog(Services.INotificationService notificationService, int minLength = 8, bool complexityRequired = true)
        {
            InitializeComponent();
            _notificationService = notificationService;
            _minLength = minLength;
            _complexityRequired = complexityRequired;

            MinLengthText.Text = $"Độ dài tối thiểu {_minLength} ký tự";
            if (!_complexityRequired)
            {
                UpperRow.Visibility = Visibility.Collapsed;
                LowerRow.Visibility = Visibility.Collapsed;
                DigitRow.Visibility = Visibility.Collapsed;
            }
            SpecialRow.Visibility = _complexityRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            string pass = NewPasswordBox.Password;
            string confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(pass) || pass.Length < _minLength)
            {
                _notificationService.ShowWarning($"Mật khẩu phải có ít nhất {_minLength} ký tự.");
                return;
            }

            if (_complexityRequired)
            {
                bool hasUpper = System.Text.RegularExpressions.Regex.IsMatch(pass, "[A-Z]");
                bool hasLower = System.Text.RegularExpressions.Regex.IsMatch(pass, "[a-z]");
                bool hasDigit = System.Text.RegularExpressions.Regex.IsMatch(pass, "[0-9]");
                bool hasSpecial = System.Text.RegularExpressions.Regex.IsMatch(pass, "[^a-zA-Z0-9]");

                if (!hasUpper || !hasLower || !hasDigit)
                {
                    _notificationService.ShowWarning("Mật khẩu phải bao gồm chữ hoa, chữ thường và số.");
                    return;
                }
                if (!hasSpecial)
                {
                    _notificationService.ShowWarning("Mật khẩu phải bao gồm ít nhất một ký tự đặc biệt.");
                    return;
                }
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
