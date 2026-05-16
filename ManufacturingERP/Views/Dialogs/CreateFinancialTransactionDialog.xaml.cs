using System.Windows;
using ManufacturingERP.ViewModels;

namespace ManufacturingERP.Views.Dialogs;

public partial class CreateFinancialTransactionDialog : Window
{
    public CreateFinancialTransactionDialog(CreateFinancialTransactionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
