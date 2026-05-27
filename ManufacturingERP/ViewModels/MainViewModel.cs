using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Services;
using ManufacturingERP.Models;

namespace ManufacturingERP.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;
    private readonly IAccessControlService _accessControlService;
    private readonly IUserPreferencesService _preferencesService;
    private readonly INotificationService _notificationService;
    private readonly ISessionMonitorService _sessionMonitorService;
    private readonly ISettingsService _settingsService;
    private readonly DashboardViewModel _dashboardViewModel;

    public INavigationService NavigationService => _navigationService;
    public INotificationService NotificationService => _notificationService;
    public User? CurrentUser => _authService.CurrentUser;
    public AIChatViewModel AIChat { get; }
    public DocumentViewModel DocumentVM { get; }

    [ObservableProperty]
    private bool _isUserLoggedIn;

    [ObservableProperty]
    private bool _isProfilePopupOpen;

    [ObservableProperty] private bool _canViewDashboard;
    [ObservableProperty] private bool _canViewSystemAdmin;
    [ObservableProperty] private bool _canViewMasterData;
    [ObservableProperty] private bool _canViewProduction;
    [ObservableProperty] private bool _canViewQualityControl;
    [ObservableProperty] private bool _canViewWarehouse;
    [ObservableProperty] private bool _canViewHumanResources;
    [ObservableProperty] private bool _canViewFinance;

    public MainViewModel(
        INavigationService navigationService, 
        IAuthService authService,
        IPermissionService permissionService,
        IAccessControlService accessControlService,
        IUserPreferencesService preferencesService,
        INotificationService notificationService,
        ISessionMonitorService sessionMonitorService,
        ISettingsService settingsService,
        DashboardViewModel dashboardViewModel,
        AIChatViewModel aiChat,
        DocumentViewModel documentVM)
    {
        _navigationService = navigationService;
        _authService = authService;
        _permissionService = permissionService;
        _accessControlService = accessControlService;
        _preferencesService = preferencesService;
        _notificationService = notificationService;
        _sessionMonitorService = sessionMonitorService;
        _settingsService = settingsService;
        _dashboardViewModel = dashboardViewModel;
        AIChat = aiChat;
        DocumentVM = documentVM;
        
        IsUserLoggedIn = false;
        
        _sessionMonitorService.SessionExpired += (s, e) => 
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _notificationService.ShowWarning("Phiên làm việc đã hết hạn do không có thao tác. Vui lòng đăng nhập lại.");
                Logout();
            });
        };

        _navigationService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(INavigationService.CurrentView))
            {
                var wasLoggedIn = IsUserLoggedIn;
                IsUserLoggedIn = _navigationService.CurrentView is not LoginViewModel;
                
                if (IsUserLoggedIn && !wasLoggedIn && CurrentUser != null)
                {
                    _ = LoadUserPermissionsAsync();
                    _ = StartSessionMonitorAsync();
                }
                else if (!IsUserLoggedIn)
                {
                    _sessionMonitorService.StopMonitoring();
                }
            }
        };

        // Check for Auto-login
        _ = InitializeSessionAsync();
    }

    private async Task StartSessionMonitorAsync()
    {
        int timeoutMinutes = await _settingsService.GetSettingIntAsync("SessionTimeout", 30);
        _sessionMonitorService.StartMonitoring(timeoutMinutes);
    }

    private async Task InitializeSessionAsync()
    {
        try
        {
        if (_preferencesService.IsRememberMe && !string.IsNullOrEmpty(_preferencesService.AutoLoginToken))
        {
            // Kiểm tra Token còn hạn trong 7 ngày không
            var expiryDate = _preferencesService.LastLoginDate.AddDays(7);
            if (System.DateTime.Now > expiryDate)
            {
                // Hết hạn 7 ngày
                _preferencesService.Clear();
                _navigationService.NavigateTo<LoginViewModel>();
                return;
            }

            // In a real app, we'd validate the token with AuthService.
            if (_preferencesService.AutoLoginToken == "valid_session")
            {
                await _authService.RestoreSessionAsync(_preferencesService.SavedUsername);
                OnPropertyChanged(nameof(CurrentUser)); // Update UI
                _navigationService.NavigateTo<DashboardViewModel>();
                return;
            }
        }

        _navigationService.NavigateTo<LoginViewModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeSession error: {ex.Message}");
            _navigationService.NavigateTo<LoginViewModel>();
        }
    }

    private async Task LoadUserPermissionsAsync()
    {
        if (CurrentUser == null) return;

        var permissions = await _permissionService.GetUserPermissionsAsync(CurrentUser.UserId);
        // Warm up shared permission cache for other viewmodels
        await _accessControlService.RefreshAsync();

        CanViewDashboard = permissions.TryGetValue(SystemModules.Dashboard, out var p1) && p1.CanView;
        CanViewSystemAdmin = permissions.TryGetValue(SystemModules.SystemAdmin, out var p2) && p2.CanView;
        CanViewMasterData = permissions.TryGetValue(SystemModules.MasterData, out var p3) && p3.CanView;
        CanViewProduction = permissions.TryGetValue(SystemModules.Production, out var p4) && p4.CanView;
        CanViewQualityControl = permissions.TryGetValue(SystemModules.QualityControl, out var p5) && p5.CanView;
        CanViewWarehouse = permissions.TryGetValue(SystemModules.Warehouse, out var p6) && p6.CanView;
        CanViewHumanResources = permissions.TryGetValue(SystemModules.HumanResources, out var p7) && p7.CanView;
        CanViewFinance = permissions.TryGetValue(SystemModules.Finance, out var p8) && p8.CanView;
    }

    [RelayCommand]
    private async Task NavigateDashboard()
    {
        _navigationService.NavigateTo<DashboardViewModel>();
        await _dashboardViewModel.InitializeAsync();
    }

    [RelayCommand]
    private void NavigateAdmin() => _navigationService.NavigateTo<AdminViewModel>();

    [RelayCommand]
    private void NavigateMasterData() => _navigationService.NavigateTo<MasterDataViewModel>();

    [RelayCommand]
    private async Task NavigateProduction()
    {
        var vm = _navigationService.NavigateTo<ProductionViewModel>();
        await vm.LoadDataAsync();
    }

    [RelayCommand]
    private void NavigateQualityControl() => _navigationService.NavigateTo<QualityControlViewModel>();

    [RelayCommand]
    private async Task NavigateWarehouse()
    {
        var vm = _navigationService.NavigateTo<WarehouseViewModel>();
        await vm.InitializeAsync();
    }

    [RelayCommand]
    private async Task NavigateHR()
    {
        var vm = _navigationService.NavigateTo<HRViewModel>();
        await vm.InitializeAsync();
    }

    [RelayCommand]
    private async Task NavigateFinance()
    {
        var vm = _navigationService.NavigateTo<FinanceViewModel>();
        await vm.InitializeAsync();
    }

    [RelayCommand]
    private async Task NavigateDocuments()
    {
        var vm = _navigationService.NavigateTo<DocumentViewModel>();
        await vm.InitializeAsync();
    }

    [RelayCommand]
    private void Logout()
    {
        IsProfilePopupOpen = false; // Close the popup
        _authService.Logout();
        OnPropertyChanged(nameof(CurrentUser)); // Ensure UI updates
        _preferencesService.Clear();
        _navigationService.NavigateTo<LoginViewModel>();
    }

}
