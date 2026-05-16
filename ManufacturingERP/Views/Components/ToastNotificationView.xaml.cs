using System.Windows;
using System.Windows.Controls;
using ManufacturingERP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ManufacturingERP.Views.Components;

public partial class ToastNotificationView : UserControl
{
    public static readonly DependencyProperty IdProperty =
        DependencyProperty.Register("Id", typeof(string), typeof(ToastNotificationView), new PropertyMetadata(string.Empty));

    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public ToastNotificationView()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var notificationService = ((App)Application.Current).Services.GetService<INotificationService>();
        notificationService?.RemoveNotification(Id);
    }
}
