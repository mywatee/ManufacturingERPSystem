using System;
using System.Windows;
using System.Windows.Controls;
using ManufacturingERP.Models;

namespace ManufacturingERP.Views.Dialogs;

public partial class StockTransactionDialog : Window
{
    public StockTransaction Transaction { get; private set; }

    public StockTransactionDialog(string defaultType = "Nhập kho")
    {
        InitializeComponent();
        
        if (defaultType == "Nhập kho")
        {
            CmbType.SelectedIndex = 0;
            TxtHeaderTitle.Text = "Tạo phiếu nhập kho";
        }
        else
        {
            CmbType.SelectedIndex = 1;
            TxtHeaderTitle.Text = "Tạo phiếu xuất kho";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;

        if (!int.TryParse(TxtMaterialId.Text, out int materialId))
        {
            ShowError("Vui lòng nhập Mã/ID vật tư hợp lệ (số nguyên).");
            return;
        }

        if (!decimal.TryParse(TxtQuantity.Text, out decimal quantity) || quantity <= 0)
        {
            ShowError("Số lượng phải lớn hơn 0.");
            return;
        }

        string type = (CmbType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Nhập kho";

        Transaction = new StockTransaction
        {
            MaterialId = materialId,
            Quantity = quantity,
            Type = type,
            ReferenceCode = TxtReference.Text,
            TransDate = DateTime.Now
        };

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
