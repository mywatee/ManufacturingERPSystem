using System.ComponentModel;
using ManufacturingERP.Core;

namespace ManufacturingERP.Services;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentView { get; }
    TViewModel NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void GoBack();
}
