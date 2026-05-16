using ManufacturingERP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ManufacturingERP.Views.Dialogs;

public partial class AddBomDialog : Window
{
    public class BomSelectionItem : INotifyPropertyChanged
    {
        public Material Material { get; set; }
        public string MaterialCode => Material.MaterialCode;
        public string MaterialName => Material.MaterialName;
        public string Unit => Material.Unit ?? "Cái";
        public int MaterialId => Material.MaterialId;

        private double _quantity;
        public double Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private readonly List<BomSelectionItem> _allSelectionItems;
    private readonly ICollectionView _selectionView;

    public List<BomSelectionItem> SelectedItems { get; private set; } = new();

    // List of existing child IDs to prevent duplicates
    public HashSet<int> ExistingChildIds { get; set; } = new();

    public AddBomDialog(IEnumerable<Material> materials)
    {
        InitializeComponent();
        
        this.MouseDown += (s, e) => 
        { 
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) 
                DragMove(); 
        };

        _allSelectionItems = materials.Select(m => new BomSelectionItem { Material = m }).ToList();
        _selectionView = CollectionViewSource.GetDefaultView(_allSelectionItems);
        _selectionView.Filter = FilterItems;
        
        GridMaterials.ItemsSource = _selectionView;
    }

    private bool FilterItems(object obj)
    {
        if (obj is not BomSelectionItem item) return false;
        
        var keyword = TxtSearch.Text.Trim();
        if (string.IsNullOrEmpty(keyword)) return true;

        return item.MaterialCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
               || item.MaterialName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _selectionView.Refresh();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SelectedItems = _allSelectionItems.Where(i => i.Quantity > 0).ToList();

        if (SelectedItems.Count == 0)
        {
            MessageBox.Show("Vui lòng nhập số lượng cho ít nhất một linh kiện.", "Thiếu thông tin", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check for duplicates with existing items
        var duplicates = SelectedItems.Where(i => ExistingChildIds.Contains(i.MaterialId)).ToList();
        if (duplicates.Any())
        {
            var msg = "Các linh kiện sau đã có trong định mức:\n" + 
                      string.Join("\n", duplicates.Select(d => $"- {d.MaterialCode}"));
            MessageBox.Show(msg, "Trùng lặp", MessageBoxButton.OK, MessageBoxImage.Warning);
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
}
