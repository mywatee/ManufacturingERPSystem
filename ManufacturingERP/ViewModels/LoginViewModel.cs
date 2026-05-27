using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ManufacturingERP.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IUserPreferencesService _preferencesService;
        private System.Threading.CancellationTokenSource? _errorTimerCts;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _infoMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isPasswordVisible;

        [ObservableProperty]
        private bool _isRememberMe;

        // === WorkOrder Stats (real data from DB) ===
        [ObservableProperty]
        private string _inProgressCount = "–";

        [ObservableProperty]
        private string _plannedCount = "–";

        [ObservableProperty]
        private string _completedTodayCount = "–";

        [ObservableProperty]
        private string _systemStatusText = "Đang kết nối...";

        [ObservableProperty]
        private string _systemStatusColor = "#60A5FA";

        public LoginViewModel(
            IAuthService authService, 
            INavigationService navigationService,
            IUserPreferencesService preferencesService)
        {
            _authService = authService;
            _navigationService = navigationService;
            _preferencesService = preferencesService;

            // Load saved preferences
            Username = _preferencesService.SavedUsername;
            IsRememberMe = _preferencesService.IsRememberMe;

            // Load real WorkOrder stats in background
            _ = LoadWorkOrderStatsAsync();
        }

        private async Task LoadWorkOrderStatsAsync()
        {
            try
            {
                await using var context = new ManufacturingContext();
                var today = DateTime.Today;

                var inProgress = await context.WorkOrders
                    .CountAsync(w => w.Status == "InProgress");

                var planned = await context.WorkOrders
                    .CountAsync(w => w.Status == "Planned");

                var completedToday = await context.WorkOrders
                    .CountAsync(w => w.Status == "Completed"
                                  && w.EndDate.HasValue
                                  && w.EndDate.Value.Date == today);

                InProgressCount = inProgress.ToString();
                PlannedCount = planned.ToString();
                CompletedTodayCount = completedToday.ToString();
                SystemStatusText = "Hệ thống hoạt động bình thường";
                SystemStatusColor = "#34D399";
            }
            catch
            {
                InProgressCount = "N/A";
                PlannedCount = "N/A";
                CompletedTodayCount = "N/A";
                SystemStatusText = "Không thể kết nối cơ sở dữ liệu";
                SystemStatusColor = "#F87171";
            }
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
                    // Save or clear preferences based on IsRememberMe
                    _preferencesService.IsRememberMe = IsRememberMe;
                    _preferencesService.SavedUsername = IsRememberMe ? Username : string.Empty;
                    
                    // Simple "token" for auto-login (for security, this should be a real token)
                    _preferencesService.AutoLoginToken = IsRememberMe ? "valid_session" : string.Empty;
                    _preferencesService.LastLoginDate = IsRememberMe ? System.DateTime.Now : System.DateTime.MinValue;
                    
                    await _preferencesService.SaveAsync();

                    _navigationService.NavigateTo<DashboardViewModel>();
                }
                else
                {
                    ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác.";
                }
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RequestReset()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Vui lòng nhập Tên đăng nhập để yêu cầu cấp lại mật khẩu.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                await Task.Delay(800);
                bool success = await _authService.RequestPasswordResetAsync(Username);
                
                if (success)
                {
                    InfoMessage = "Yêu cầu cấp lại mật khẩu đã được gửi.\nVui lòng liên hệ Admin để nhận mật khẩu mới.";
                }
                else
                {
                    ErrorMessage = "Không tìm thấy tên đăng nhập này trong hệ thống.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = "Lỗi khi gửi yêu cầu: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnErrorMessageChanged(string? value) => StartMessageTimer(ref _errorTimerCts, value, v => ErrorMessage = v);

        partial void OnInfoMessageChanged(string? value) => StartMessageTimer(ref _infoTimerCts, value, v => InfoMessage = v);

        private System.Threading.CancellationTokenSource? _infoTimerCts;

        private void StartMessageTimer(ref System.Threading.CancellationTokenSource? cts, string? value, System.Action<string> clearAction)
        {
            cts?.Cancel();
            if (!string.IsNullOrEmpty(value))
            {
                cts = new System.Threading.CancellationTokenSource();
                var token = cts.Token;

                Task.Delay(5000, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        clearAction(string.Empty);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }
}
