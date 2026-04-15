using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ManufacturingERP.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ManufacturingERP.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentView = viewModel;
    }
}
