using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace ManufacturingERP.Views.Dialogs;

public partial class MaterialDialog : Window
{
    public string MaterialId => TxtId.Text.Trim();
    public string MaterialName => TxtName.Text.Trim();
    public string Unit => TxtUnit.Text.Trim();
    public string Category => CmbCategory.SelectedItem?.ToString() ?? "Nguyên liệu";
    public string Status => CmbStatus.SelectedItem?.ToString() ?? "Đang sử dụng";
    public int OnHand => (int)Math.Round(TryParseDouble(TxtOnHand.Text));
    public int MinStock => (int)Math.Round(TryParseDouble(TxtMinStock.Text));
    public decimal UnitPrice => (decimal)TryParseDouble(TxtUnitPrice.Text);

    public MaterialDialog(string suggestedId)
    {
        InitializeComponent();
        
        // Window drag logic
        this.MouseDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

        TxtId.Text = suggestedId;
        TxtUnit.Text = "Kg";

        CmbCategory.ItemsSource = new[] { "Nguyên liệu", "Bán thành phẩm", "Thành phẩm" };
        CmbCategory.SelectedIndex = 0;

        CmbStatus.ItemsSource = new[] { "Đang sử dụng", "Ngừng sử dụng" };
        CmbStatus.SelectedIndex = 0;

        TxtOnHand.Text = "0";
        TxtMinStock.Text = "10";
        TxtUnitPrice.Text = "0";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MaterialId) || string.IsNullOrWhiteSpace(MaterialName))
        {
            MessageBox.Show(this, "Vui lòng nhập Mã vật tư và Tên vật tư.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static double TryParseDouble(string text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.GetCultureInfo("vi-VN"), out value))
        {
            return value;
        }

        return 0;
    }
}
