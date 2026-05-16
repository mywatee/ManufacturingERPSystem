using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ManufacturingERP.Models;

public partial class Attendance : ObservableObject
{
    [ObservableProperty]
    private int _attendanceId;

    [ObservableProperty]
    private int _employeeId;

    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private TimeSpan? _checkIn;

    [ObservableProperty]
    private TimeSpan? _checkOut;

    [ObservableProperty]
    private string? _status; // Present, Late, Absent, OnLeave

    [ObservableProperty]
    private string? _note;

    public string WorkHours 
    {
        get 
        {
            if (CheckIn == null || CheckOut == null) return "0 giờ";
            var duration = CheckOut.Value - CheckIn.Value;
            return $"{duration.TotalHours:N1} giờ";
        }
    }

    public string Overtime
    {
        get
        {
            double ot = OvertimeHours;
            return ot > 0 ? $"+{ot:N1} giờ" : "0 giờ";
        }
    }

    public double OvertimeHours
    {
        get
        {
            if (CheckIn == null || CheckOut == null) return 0;
            var duration = CheckOut.Value - CheckIn.Value;
            var ot = duration.TotalHours - 8;
            return ot > 0 ? ot : 0;
        }
    }

    public virtual Employee Employee { get; set; } = null!;
}
