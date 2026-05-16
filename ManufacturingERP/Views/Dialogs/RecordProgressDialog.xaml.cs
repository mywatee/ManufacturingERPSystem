using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ManufacturingERP.Views.Dialogs
{
    public partial class RecordProgressDialog : Window
    {
        public int ActualQty => int.TryParse(TxtActualQty.Text, out int q) ? q : 0;
        public int FailedQty => int.TryParse(TxtFailedQty.Text, out int q) ? q : 0;
        public string Stage => (CmbStage.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "N/A";
        public string Operator => TxtOperator.Text;
        public string Notes => TxtNotes.Text;

        public RecordProgressDialog(string wocode, string operatorName = "")
        {
            InitializeComponent();
            TxtSubtitle.Text = wocode;
            TxtOperator.Text = string.IsNullOrEmpty(operatorName) ? "Hệ thống" : operatorName;
            TxtActualQty.Focus();
            
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (ActualQty <= 0 && FailedQty <= 0)
            {
                MessageBox.Show("Vui lòng nhập ít nhất một số lượng hợp lệ (đạt hoặc lỗi).", 
                    "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
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
