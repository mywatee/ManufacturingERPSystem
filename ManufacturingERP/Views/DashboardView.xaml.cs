using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using ManufacturingERP.ViewModels;

namespace ManufacturingERP.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            // Fix Tooltip to show only the hovered series
            if (PieChartDefect.DataTooltip is DefaultTooltip tooltip)
            {
                tooltip.SelectionMode = TooltipSelectionMode.OnlySender;
            }
        }

        private void PieChart_DataClick(object sender, ChartPoint chartPoint)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                viewModel.UpdateSelectedDefectCommand.Execute(chartPoint);
            }
        }

        private void PieChart_DataHover(object sender, ChartPoint chartPoint)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                viewModel.UpdateSelectedDefectCommand.Execute(chartPoint);
            }
        }

        private void PieChart_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                viewModel.UpdateSelectedDefectCommand.Execute(null);
            }
        }
    }
}
