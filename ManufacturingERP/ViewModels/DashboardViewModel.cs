using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;
using ManufacturingERP.Core;

namespace ManufacturingERP.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    // Stats
    [ObservableProperty] private string _runningOrders = "24";
    [ObservableProperty] private string _materialAlerts = "8";
    [ObservableProperty] private string _todayProductivity = "94.2%";
    [ObservableProperty] private string _monthlyRevenue = "1.2 tỷ";

    // Charts
    [ObservableProperty] private SeriesCollection? _productionSeries;
    [ObservableProperty] private SeriesCollection? _defectSeries;
    [ObservableProperty] private string[]? _productionLabels;

    // Tables
    public ObservableCollection<UrgentOrder> UrgentOrders { get; } = new();
    public ObservableCollection<RecentActivity> Activities { get; } = new();

    [ObservableProperty]
    private string _selectedDefectTitle = "Sản phẩm lỗi";

    [ObservableProperty]
    private double _selectedDefectValue = 100;

    public DashboardViewModel()
    {
        LoadMockData();
    }

    private void LoadMockData()
    {
        // 1. Line Chart: Production Progress vs Plan (Premium Style)
        ProductionSeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Thực tế",
                Values = new ChartValues<double> { 210, 240, 255, 260, 265, 240, 190 },
                Stroke = System.Windows.Media.Brushes.DodgerBlue,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 30, 144, 255)),
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10,
                PointForeground = System.Windows.Media.Brushes.White
            },
            new LineSeries
            {
                Title = "Kế hoạch",
                Values = new ChartValues<double> { 230, 235, 245, 250, 245, 230, 200 },
                Stroke = System.Windows.Media.Brushes.Tomato,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 99, 71)),
                PointGeometry = DefaultGeometries.Square,
                PointGeometrySize = 10,
                PointForeground = System.Windows.Media.Brushes.White
            }
        };
        ProductionLabels = new[] { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

        // 2. Pie Chart: Defect Reasons (Clean Modern Style)
        DefectSeries = new SeriesCollection
        {
            new PieSeries 
            { 
                Title = "Lỗi kỹ thuật", 
                Values = new ChartValues<double> { 35 }, 
                DataLabels = false, 
                Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#EF4444")!
            },
            new PieSeries 
            { 
                Title = "Kích thước sai lệch", 
                Values = new ChartValues<double> { 28 }, 
                DataLabels = false, 
                Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#F97316")!
            },
            new PieSeries 
            { 
                Title = "Bề mặt không đạt", 
                Values = new ChartValues<double> { 22 }, 
                DataLabels = false, 
                Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#F59E0B")!
            },
            new PieSeries 
            { 
                Title = "Nguyên liệu kém", 
                Values = new ChartValues<double> { 15 }, 
                DataLabels = false, 
                Fill = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#EAB308")!
            }
        };

        // 3. Urgent Orders
        UrgentOrders.Add(new UrgentOrder { Code = "LSX-2026-0413", Product = "Bánh răng truyền động BR-45", Quantity = 500, Progress = 64, Status = "Đang làm", IsUrgent = true, Deadline = "15/04/2026" });
        UrgentOrders.Add(new UrgentOrder { Code = "LSX-2026-0412", Product = "Trục cam TC-28A", Quantity = 300, Progress = 0, Status = "Chờ", IsUrgent = false, Deadline = "18/04/2026" });
        UrgentOrders.Add(new UrgentOrder { Code = "LSX-2026-0410", Product = "Vỏ máy bơm VB-120", Quantity = 200, Progress = 93, Status = "Đang làm", IsUrgent = true, Deadline = "14/04/2026" });
        UrgentOrders.Add(new UrgentOrder { Code = "LSX-2026-0408", Product = "Piston thủy lực PT-65", Quantity = 400, Progress = 100, Status = "Hoàn thành", IsUrgent = false, Deadline = "12/04/2026" });

        // 4. Recent Activities
        Activities.Add(new RecentActivity { Type = "Thêm", Content = "Lệnh sản xuất LSX-2026-0413", User = "Huy Hoàng", Time = "10:30 SA" });
        Activities.Add(new RecentActivity { Type = "Sửa", Content = "Định mức BOM-SP-1025", User = "Mai Anh", Time = "09:45 SA" });
        Activities.Add(new RecentActivity { Type = "Xóa", Content = "Phiếu xuất kho PXK-0412", User = "Tuấn Minh", Time = "08:20 SA" });
        Activities.Add(new RecentActivity { Type = "Thêm", Content = "Nhà cung cấp NCC-VL-089", User = "Thu Hà", Time = "07:55 SA" });
        Activities.Add(new RecentActivity { Type = "Sửa", Content = "Hồ sơ nhân viên NV-2024-156", User = "Huy Hoàng", Time = "07:30 SA" });
    }

    [RelayCommand]
    private void UpdateSelectedDefect(ChartPoint point)
    {
        if (point == null)
        {
            SelectedDefectTitle = "Sản phẩm lỗi";
            SelectedDefectValue = 100;
            return;
        }

        SelectedDefectTitle = point.SeriesView.Title;
        SelectedDefectValue = point.Y;
    }
}

public class UrgentOrder
{
    public string? Code { get; set; }
    public string? Product { get; set; }
    public int Quantity { get; set; }
    public int Progress { get; set; }
    public string? Status { get; set; }
    public bool IsUrgent { get; set; }
    public string? Deadline { get; set; }
}

public class RecentActivity
{
    public string? Type { get; set; }
    public string? Content { get; set; }
    public string? User { get; set; }
    public string? Time { get; set; }
}
