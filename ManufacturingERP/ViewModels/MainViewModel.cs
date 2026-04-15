using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;

    public INavigationService NavigationService => _navigationService;

    [ObservableProperty]
    private bool _isUserLoggedIn;

    public MainViewModel(INavigationService navigationService, IAuthService authService)
    {
        _navigationService = navigationService;
        _authService = authService;
        
        // Trạng thái ban đầu: Chưa đăng nhập
        IsUserLoggedIn = false;

        // Update shell visibility based on current view
        _navigationService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(INavigationService.CurrentView))
            {
                IsUserLoggedIn = _navigationService.CurrentView is not LoginViewModel;
            }
        };

        // Start at Login screen
        _navigationService.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void NavigateDashboard() => _navigationService.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void NavigateAdmin() => _navigationService.NavigateTo<AdminViewModel>();

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        _navigationService.NavigateTo<LoginViewModel>();
    }
}
