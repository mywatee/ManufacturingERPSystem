using System.Globalization;
using System.Windows;

namespace ManufacturingERP.Views.Dialogs;

public partial class BomLineDialog : Window
{
    public string ComponentCode => TxtCode.Text.Trim();
    public string ComponentName => TxtName.Text.Trim();
    public double QuantityPer => TryParseDouble(TxtQty.Text);
    public string Unit => TxtUnit.Text.Trim();
    public double ScrapRate => TryParseDouble(TxtScrap.Text);

    public BomLineDialog(string suggestedCode, string suggestedUnit)
    {
        InitializeComponent();
        TxtCode.Text = suggestedCode;
        TxtUnit.Text = suggestedUnit;
        TxtQty.Text = "1";
        TxtScrap.Text = "0";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ComponentCode) || string.IsNullOrWhiteSpace(ComponentName))
        {
            MessageBox.Show(this, "Vui lòng nhập Mã và Tên thành phần.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
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

