using System.Windows;
using ManufacturingERP.Models;

namespace ManufacturingERP.Views.Dialogs;

public partial class PartnerDialog : Window
{
    public Partner? Partner { get; private set; }

    public PartnerDialog(Partner? existingPartner = null)
    {
        InitializeComponent();
        
        if (existingPartner != null)
        {
            Partner = existingPartner;
            TitleText.Text = "Chỉnh sửa đối tác";
            TxtCode.Text = existingPartner.PartnerCode;
            TxtName.Text = existingPartner.PartnerName;
            TxtPhone.Text = existingPartner.Phone;
            TxtEmail.Text = existingPartner.Email;
            TxtAddress.Text = existingPartner.Address;
            TxtTaxCode.Text = existingPartner.TaxCode;
            
            foreach (System.Windows.Controls.ComboBoxItem item in CmbType.Items)
            {
                if (item.Content.ToString() == existingPartner.PartnerType)
                {
                    CmbType.SelectedItem = item;
                    break;
                }
            }
        }
        else
        {
            Partner = new Partner();
        }

        this.MouseDown += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(TxtCode.Text) || string.IsNullOrWhiteSpace(TxtName.Text))
        {
            AlertDialog.Show("Vui lòng nhập đầy đủ Mã và Tên đối tác.", "Lỗi nhập liệu", AlertType.Warning, this);
            return;
        }

        // Email validation (optional but if provided must be valid)
        if (!string.IsNullOrWhiteSpace(TxtEmail.Text))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(TxtEmail.Text);
                if (addr.Address != TxtEmail.Text) throw new System.Exception();
            }
            catch
            {
                AlertDialog.Show("Định dạng Email không hợp lệ.", "Lỗi nhập liệu", AlertType.Warning, this);
                return;
            }
        }

        // Phone validation (optional but should be numeric if provided)
        if (!string.IsNullOrWhiteSpace(TxtPhone.Text))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(TxtPhone.Text, @"^[0-9+\s-]{8,15}$"))
            {
                AlertDialog.Show("Số điện thoại không hợp lệ (yêu cầu từ 8-15 chữ số).", "Lỗi nhập liệu", AlertType.Warning, this);
                return;
            }
        }

        Partner!.PartnerCode = TxtCode.Text.Trim().ToUpper();
        Partner.PartnerName = TxtName.Text.Trim();
        Partner.PartnerType = (CmbType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
        Partner.Phone = TxtPhone.Text.Trim();
        Partner.Email = TxtEmail.Text.Trim();
        Partner.Address = TxtAddress.Text.Trim();
        Partner.TaxCode = TxtTaxCode.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
