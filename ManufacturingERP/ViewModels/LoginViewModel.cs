using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Services;
using System.Threading.Tasks;

namespace ManufacturingERP.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isPasswordVisible;

        public LoginViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Artificial delay for premium feel (loading spinner)
                await Task.Delay(800);

                bool success = await _authService.LoginAsync(Username, Password);

                if (success)
                {
                    _navigationService.NavigateTo<DashboardViewModel>();
                }
                else
                {
                    ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác.";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
