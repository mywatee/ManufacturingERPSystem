using System.ComponentModel;
using ManufacturingERP.Core;

namespace ManufacturingERP.Services;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentView { get; }
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
}
