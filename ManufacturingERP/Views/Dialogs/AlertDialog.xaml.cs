using System.Windows;
using System.Windows.Media;

namespace ManufacturingERP.Views.Dialogs;

public partial class AlertDialog : Window
{
    public AlertDialog(string message, string title = "Thông báo", AlertType type = AlertType.Info)
    {
        InitializeComponent();
        TxtTitle.Text = title;
        TxtMessage.Text = message;
        
        switch (type)
        {
            case AlertType.Success:
                IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
                IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
                IconText.Text = "\uE73E"; // Checkmark
                break;
            case AlertType.Warning:
                IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB"));
                IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                IconText.Text = "\uE7BA"; // Warning
                break;
            case AlertType.Error:
                IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF2F2"));
                IconText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                IconText.Text = "\uE711"; // Error/Cancel
                break;
        }

        this.MouseDown += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public static void Show(string message, string title = "Thông báo", AlertType type = AlertType.Info, Window owner = null)
    {
        var dialog = new AlertDialog(message, title, type);
        if (owner != null) dialog.Owner = owner;
        else if (Application.Current?.MainWindow != null) dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
    }
}

public enum AlertType
{
    Info,
    Success,
    Warning,
    Error
}
