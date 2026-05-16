using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public class NotificationService : INotificationService
{
    public ObservableCollection<NotificationInfo> ActiveNotifications { get; } = new();

    public void ShowSuccess(string message) => AddNotification(message, NotificationType.Success);
    public void ShowError(string message) => AddNotification(message, NotificationType.Error);
    public void ShowWarning(string message) => AddNotification(message, NotificationType.Warning);
    public void ShowInfo(string message) => AddNotification(message, NotificationType.Info);

    public bool Confirm(string message, string title = "Xác nhận")
    {
        var dialog = new Views.Dialogs.ConfirmDialog(message, title)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        return dialog.ShowDialog() == true;
    }

    public void RemoveNotification(string id)
    {
        var notification = ActiveNotifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            ActiveNotifications.Remove(notification);
        }
    }

    private void AddNotification(string message, NotificationType type)
    {
        var notification = new NotificationInfo
        {
            Message = message,
            Type = type
        };

        // UI update must be on UI thread
        if (App.Current != null)
        {
            App.Current.Dispatcher.Invoke(() => ActiveNotifications.Add(notification));
        }

        // Auto-remove after duration
        Task.Delay(TimeSpan.FromSeconds(notification.DurationSeconds)).ContinueWith(_ =>
        {
            if (App.Current != null)
            {
                App.Current.Dispatcher.Invoke(() => RemoveNotification(notification.Id));
            }
        });
    }
}
