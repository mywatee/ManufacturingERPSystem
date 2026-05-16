using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ManufacturingERP.Models;

public partial class User : ObservableObject
{
    [ObservableProperty]
    private int _userId;

    [ObservableProperty]
    private string _username = null!;

    [NotMapped]
    public string FullName => Employee?.FullName ?? Username;

    [ObservableProperty]
    private string _passwordHash = null!;

    [ObservableProperty]
    private bool? _isActive;

    [ObservableProperty]
    private int _failedLoginAttempts;

    [ObservableProperty]
    private DateTime? _lockoutEnd;

    [ObservableProperty]
    private int? _employeeId;

    [ForeignKey("EmployeeId")]
    public virtual Employee? Employee { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new List<EmployeeSchedule>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<QualityControl> QualityControls { get; set; } = new List<QualityControl>();

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    public virtual ICollection<WorkOrderProgress> WorkOrderProgresses { get; set; } = new List<WorkOrderProgress>();

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<PasswordResetRequest> PasswordResetRequests { get; set; } = new List<PasswordResetRequest>();
}
