using System.Collections.Generic;
using System.Collections.ObjectModel;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface INotificationService
{
    ObservableCollection<NotificationInfo> ActiveNotifications { get; }
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
    void RemoveNotification(string id);
    bool Confirm(string message, string title = "Xác nhận");
}
