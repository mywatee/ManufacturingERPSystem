using System.Windows;

namespace ManufacturingERP.Views.Dialogs;

public partial class ImportDialog : Window
{
    public ImportDialog()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
