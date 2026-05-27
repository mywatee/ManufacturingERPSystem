using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ManufacturingERP.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ManufacturingERP.Views.Dialogs;

public partial class WarehouseConfigDialog : Window
{
    public Warehouse NewWarehouse { get; private set; }
    private readonly Warehouse? _existing;

    public WarehouseConfigDialog(Warehouse? existing = null)
    {
        InitializeComponent();
        _existing = existing;
        LoadManagers();
        if (_existing != null) PopulateFields();
    }

    private void PopulateFields()
    {
        Title = "Sửa nhà kho";
        TxtCode.Text = _existing!.Code;
        TxtWarehouseName.Text = _existing.WarehouseName;
        TxtLocation.Text = _existing.Location;
        TxtCapacity.Text = _existing.Capacity?.ToString();
        foreach (ComboBoxItem item in CmbStatus.Items)
            if (item.Content?.ToString() == _existing.Status) { CmbStatus.SelectedItem = item; break; }
        if (_existing.ManagerId.HasValue) CmbManager.SelectedValue = _existing.ManagerId;
    }

    private void LoadManagers()
    {
        try
        {
            var factory = ((App)Application.Current).Services
                .GetRequiredService<IDbContextFactory<ManufacturingContext>>();
            using var context = factory.CreateDbContext();
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

        if (_existing != null)
        {
            _existing.Code = TxtCode.Text?.Trim();
            _existing.WarehouseName = TxtWarehouseName.Text.Trim();
            _existing.Location = TxtLocation.Text.Trim();
            _existing.Capacity = capacity;
            _existing.Status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Hoạt động";
            _existing.ManagerId = (int?)CmbManager.SelectedValue;
            NewWarehouse = _existing;
        }
        else
        {
            NewWarehouse = new Warehouse
            {
                Code = TxtCode.Text?.Trim(),
                WarehouseName = TxtWarehouseName.Text.Trim(),
                Location = TxtLocation.Text.Trim(),
                Capacity = capacity,
                Status = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Hoạt động",
                ManagerId = (int?)CmbManager.SelectedValue
            };
        }

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}
