using System.Windows;

namespace ManufacturingERP.Views.Dialogs;

public partial class EditBomDialog : Window
{
    public double Quantity { get; private set; }

    public EditBomDialog(string componentCode, string componentName, double currentQuantity, string unit)
    {
        InitializeComponent();
        
        // Use a simple dictionary or dynamic for DataContext to avoid read-only anonymous type issues in XAML
        DataContext = new { ComponentCode = componentCode, ComponentName = componentName, Unit = unit };
        TxtQuantity.Text = currentQuantity.ToString();
        
        this.MouseDown += (s, e) => 
        { 
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) 
                DragMove(); 
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(TxtQuantity.Text, out double qty) && qty > 0)
        {
            Quantity = qty;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Vui lòng nhập số lượng hợp lệ (lớn hơn 0).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
