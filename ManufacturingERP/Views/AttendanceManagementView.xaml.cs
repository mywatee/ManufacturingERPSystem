using System.Windows.Controls;

namespace ManufacturingERP.Views
{
    public partial class AttendanceManagementView : UserControl
    {
        public AttendanceManagementView()
        {
            InitializeComponent();
        }

        private void AttendanceGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "EmployeeId" || e.PropertyName == "IsSelected")
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "STT")
            {
                e.Column.Width = 50;
                e.Column.Header = "STT";
            }
            else if (e.PropertyName == "EmployeeInfo")
            {
                e.Column.Header = "Nhân viên";
                e.Column.Width = 200;
            }
            else if (e.PropertyName == "Phòng ban")
            {
                e.Column.Header = "Phòng ban";
                e.Column.Width = 150;
            }
            else
            {
                // Day columns
                e.Column.Width = 40;
                e.Column.Header = e.PropertyName;
            }
        }
    }
}
