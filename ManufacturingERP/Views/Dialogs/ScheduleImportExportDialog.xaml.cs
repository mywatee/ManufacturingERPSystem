using System.Windows;

namespace ManufacturingERP.Views.Dialogs;

public partial class ScheduleImportExportDialog : Window
{
    public ScheduleImportExportDialog()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
