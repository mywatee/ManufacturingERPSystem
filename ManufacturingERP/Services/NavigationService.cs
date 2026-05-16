using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ManufacturingERP.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ManufacturingERP.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly System.Collections.Generic.Stack<ViewModelBase> _history = new();

    [ObservableProperty]
    private ViewModelBase? _currentView;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TViewModel NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        if (CurrentView != null)
        {
            _history.Push(CurrentView);
        }

        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentView = viewModel;
        return viewModel;
    }

    public void GoBack()
    {
        if (_history.Count > 0)
        {
            CurrentView = _history.Pop();
        }
    }
}
