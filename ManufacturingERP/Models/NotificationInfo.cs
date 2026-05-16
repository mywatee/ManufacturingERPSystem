using System;

namespace ManufacturingERP.Models;

public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}

public class NotificationInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public double DurationSeconds { get; set; } = 4.0;
}
