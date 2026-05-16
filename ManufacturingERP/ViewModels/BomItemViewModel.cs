using CommunityToolkit.Mvvm.ComponentModel;

namespace ManufacturingERP.ViewModels;

public partial class BomItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _materialId = "";

    [ObservableProperty]
    private string _materialCode = "";

    [ObservableProperty]
    private string _materialName = "";

    [ObservableProperty]
    private double _quantity = 1;

    [ObservableProperty]
    private string _unit = "Kg";

    public string QuantityDisplay => $"{Quantity} {Unit}";
}
