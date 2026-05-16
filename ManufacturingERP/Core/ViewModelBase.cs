using CommunityToolkit.Mvvm.ComponentModel;

namespace ManufacturingERP.Core;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    public virtual Task InitializeAsync() => Task.CompletedTask;
}
