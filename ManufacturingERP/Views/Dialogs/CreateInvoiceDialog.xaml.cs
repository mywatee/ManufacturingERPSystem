using System.Windows;
using ManufacturingERP.ViewModels;

namespace ManufacturingERP.Views.Dialogs;

public partial class CreateInvoiceDialog : Window
{
    public CreateInvoiceDialog(CreateInvoiceViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
