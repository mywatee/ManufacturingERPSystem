using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using ManufacturingERP.Core;
using System;

namespace ManufacturingERP.ViewModels;

public partial class ActivityDetailViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IProductionService _productionService;

    private RecentActivity? _selectedActivity;
    public RecentActivity? SelectedActivity
    {
        get => _selectedActivity;
        set => SetProperty(ref _selectedActivity, value);
    }

    private string _badgeColor = "#DCFCE7";
    public string BadgeColor
    {
        get => _badgeColor;
        set => SetProperty(ref _badgeColor, value);
    }

    private string _textColor = "#166534";
    public string TextColor
    {
        get => _textColor;
        set => SetProperty(ref _textColor, value);
    }

    public ActivityDetailViewModel(INavigationService navigationService, IProductionService productionService)
    {
        _navigationService = navigationService;
        _productionService = productionService;
    }

    public void Initialize(RecentActivity activity)
    {
        SelectedActivity = activity;
        
        // Setup colors based on activity type to match Dashboard styling
        switch (activity.Type)
        {
            case "Thêm":
            case "Bắt đầu":
            case "Đang sản xuất":
            case "Hoàn thành":
            case "Phê duyệt":
                BadgeColor = "#DCFCE7"; // Green
                TextColor = "#166534";
                break;
            case "Sửa":
            case "Cập nhật":
                BadgeColor = "#DBEAFE"; // Blue
                TextColor = "#1E40AF";
                break;
            case "Tạm dừng":
            case "Chờ duyệt":
                BadgeColor = "#FFEDD5"; // Orange
                TextColor = "#9A3412";
                break;
            case "Xóa":
            case "Hủy":
                BadgeColor = "#FEE2E2"; // Red
                TextColor = "#991B1B";
                break;
            default:
                BadgeColor = "#F1F5F9"; // Slate/Gray
                TextColor = "#475569";
                break;
        }
    }

    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Close()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private void ViewRelatedObject()
    {
        if (SelectedActivity?.Content?.Contains("LSX") == true)
        {
            _navigationService.NavigateTo<DashboardViewModel>();
            RequestClose?.Invoke(); // Close dialog after navigation
        }
    }
}
