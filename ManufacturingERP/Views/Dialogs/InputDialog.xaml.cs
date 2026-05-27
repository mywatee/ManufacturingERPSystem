using System.Windows;

namespace ManufacturingERP.Views.Dialogs;

public partial class InputDialog : Window
{
    public string? InputText => InputTextBox.Text;

    public InputDialog(string title, string message, string defaultValue = "")
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        InputTextBox.Text = defaultValue;
        InputTextBox.Focus();
    }
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
