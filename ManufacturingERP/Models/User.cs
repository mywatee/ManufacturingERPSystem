using System;
using System.Collections.Generic;

namespace ManufacturingERP.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new List<EmployeeSchedule>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<QualityControl> QualityControls { get; set; } = new List<QualityControl>();

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    public virtual ICollection<WorkOrderProgress> WorkOrderProgresses { get; set; } = new List<WorkOrderProgress>();

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
