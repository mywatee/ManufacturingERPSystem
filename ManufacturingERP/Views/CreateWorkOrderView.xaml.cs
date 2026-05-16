using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace ManufacturingERP.Views;

public partial class CreateWorkOrderView : UserControl
{
    public CreateWorkOrderView()
    {
        InitializeComponent();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        // Only allow numbers
        e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
    }

    private void OnBackgroundMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            if (IsClickInsideInteractiveControl(source))
            {
                return;
            }
        }

        if (DataContext is ViewModels.CreateWorkOrderViewModel vm)
        {
            vm.SelectedOrderItem = null;
        }
    }

    private bool IsClickInsideInteractiveControl(DependencyObject source)
    {
        DependencyObject parent = source;
        while (parent != null)
        {
            // Check for broader interactive control classes
            if (parent is ButtonBase || 
                parent is Selector || 
                parent is TextBoxBase || 
                parent is DatePicker || 
                parent is CheckBox ||
                parent is Calendar ||
                parent is ComboBoxItem ||
                parent is DataGridCell ||
                parent is DataGridRow)
            {
                return true;
            }

            // Handle Popup traversal (critical for dropdowns and pickers)
            if (parent is Popup popup)
            {
                parent = popup.PlacementTarget;
                continue;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }
        return false;
    }
}
