using System.Windows;

namespace ManufacturingERP.Views.Dialogs;

public partial class ImportExportDialog : Window
{
    public ImportExportDialog()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
