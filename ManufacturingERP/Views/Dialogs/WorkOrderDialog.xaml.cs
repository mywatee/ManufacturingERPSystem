using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ManufacturingERP.Models;
using System.Collections.Generic;

namespace ManufacturingERP.Views.Dialogs
{
    public partial class WorkOrderDialog : Window
    {
        public string WOCode => TxtWOCode.Text;
        public Material? SelectedProduct => CmbProduct.SelectedItem as Material;
        public int Quantity => int.TryParse(TxtQuantity.Text, out int q) ? q : 0;
        public DateTime? EndDate => DpEndDate.SelectedDate;
        public bool IsUrgent => ChkIsUrgent.IsChecked ?? false;

        public WorkOrderDialog(string suggestedCode, List<Material> products, bool defaultUrgent = false)
        {
            InitializeComponent();
            TxtWOCode.Text = suggestedCode;
            CmbProduct.ItemsSource = products;
            ChkIsUrgent.IsChecked = defaultUrgent;
            DpEndDate.SelectedDate = DateTime.Now.AddDays(7);

            SetupDrag();
        }

        public WorkOrderDialog(WorkOrder order, List<Material> products)
        {
            InitializeComponent();
            TxtHeaderTitle.Text = "Chỉnh sửa Lệnh sản xuất";
            TxtWOCode.Text = order.Wocode;
            TxtWOCode.IsReadOnly = true; // Don't allow changing the code
            CmbProduct.ItemsSource = products;
            
            // Select existing product
            var productId = order.ProductId ?? order.WorkOrderItems?.FirstOrDefault()?.ProductId;
            foreach (var p in products)
            {
                if (p.MaterialId == productId)
                {
                    CmbProduct.SelectedItem = p;
                    break;
                }
            }

            TxtQuantity.Text = (order.TargetQty ?? order.WorkOrderItems?.Sum(i => i.TargetQty) ?? 0).ToString();
            DpEndDate.SelectedDate = order.EndDate;
            ChkIsUrgent.IsChecked = order.IsUrgent;
            BtnSave.Content = "Lưu thay đổi";

            SetupDrag();
        }

        private void SetupDrag()
        {
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Quantity <= 0)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EndDate == null || EndDate < DateTime.Today)
            {
                MessageBox.Show("Ngày hoàn thành không hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
