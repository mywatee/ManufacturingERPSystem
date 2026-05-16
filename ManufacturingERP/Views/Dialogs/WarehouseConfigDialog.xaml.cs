using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;

namespace ManufacturingERP.Views.Dialogs;

public partial class WarehouseConfigDialog : Window
{
    public Warehouse NewWarehouse { get; private set; }

    public WarehouseConfigDialog()
    {
        InitializeComponent();
        LoadManagers();
    }

    private void LoadManagers()
    {
        try
        {
            using var context = new ManufacturingContext();
            var managers = context.Users.Where(u => u.IsActive == true).ToList();
            CmbManager.ItemsSource = managers;
        }
        catch (Exception) { /* Handle silently for dialog */ }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(TxtWarehouseName.Text))
        {
            ShowError("Vui lòng nhập tên nhà kho.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtLocation.Text))
        {
            ShowError("Vui lòng nhập vị trí nhà kho.");
            return;
        }
        
        decimal capacity = 0;
        if (!decimal.TryParse(TxtCapacity.Text, out capacity) || capacity < 0)
        {
            ShowError("Vui lòng nhập công suất hợp lệ.");
            return;
        }

        NewWarehouse = new Warehouse
        {
            Code = TxtCode.Text?.Trim(),
            WarehouseName = TxtWarehouseName.Text.Trim(),
            Location = TxtLocation.Text.Trim(),
            Capacity = capacity,
            Status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Hoạt động",
            ManagerId = (int?)CmbManager.SelectedValue
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
