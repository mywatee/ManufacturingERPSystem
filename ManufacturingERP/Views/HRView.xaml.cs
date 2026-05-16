using System.Windows.Controls;

namespace ManufacturingERP.Views;

public partial class HRView : UserControl
{
    public HRView()
    {
        InitializeComponent();
    }

    private void ScheduleGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.PropertyName == "EmployeeId" || e.PropertyName == "InternalUserId") { e.Cancel = true; return; }
        
        if (e.PropertyName == "Nhân viên")
        {
            e.Column.Width = 180;
            e.Column.Header = "Nhân viên";
        }
        else if (e.PropertyName == "Mã NV")
        {
            e.Column.Width = 80;
            e.Column.Header = "Mã NV";
        }
        else if (e.PropertyName == "Ca làm việc")
        {
            e.Column.Width = 120;
            e.Column.Header = "Ca làm việc";
        }
        else
        {
            // Day columns
            e.Column.Width = 40;
            e.Column.Header = e.PropertyName;
        }
    }
}
