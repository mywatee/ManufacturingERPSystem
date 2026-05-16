using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ManufacturingERP.Models;

namespace ManufacturingERP.Views.Dialogs;

public partial class ShiftDialog : Window
{
    public Shift Shift { get; private set; }
    public string SelectedColorHex { get; private set; } = "#2563EB";

    private readonly List<string> _premiumColors = new()
    {
        "#2563EB", // Blue
        "#4F46E5", // Indigo
        "#7C3AED", // Purple
        "#C026D3", // Fuchsia
        "#DB2777", // Pink
        "#E11D48", // Rose
        "#DC2626", // Red
        "#EA580C", // Orange
        "#D97706", // Amber
        "#65A30D", // Lime
        "#16A34A", // Emerald
        "#0891B2", // Cyan
        "#0284C7", // Sky
        "#475569"  // Slate
    };

    public ShiftDialog(Shift? shift = null)
    {
        InitializeComponent();
        Shift = shift ?? new Shift { IsActive = true, ColorHex = "#2563EB" };
        
        if (shift != null)
        {
            DialogTitle.Text = "CHỈNH SỬA CA LÀM VIỆC";
            NameTextBox.Text = Shift.ShiftName;
            StartTimeTextBox.Text = Shift.StartTime?.ToString("HH:mm") ?? "08:00";
            EndTimeTextBox.Text = Shift.EndTime?.ToString("HH:mm") ?? "17:00";
            BreakStartTextBox.Text = Shift.BreakStartTime?.ToString("HH:mm") ?? "12:00";
            BreakEndTextBox.Text = Shift.BreakEndTime?.ToString("HH:mm") ?? "13:00";
            IsActiveCheckBox.IsChecked = Shift.IsActive;
            SelectedColorHex = Shift.ColorHex ?? "#2563EB";
        }

        LoadColors();
    }

    private void LoadColors()
    {
        foreach (var hex in _premiumColors)
        {
            var border = new Border
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 0, 12, 12),
                Background = (Brush)new BrushConverter().ConvertFromString(hex),
                CornerRadius = new CornerRadius(16),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = hex,
                BorderThickness = hex == SelectedColorHex ? new Thickness(2) : new Thickness(0),
                BorderBrush = Brushes.Black
            };
            border.MouseDown += (s, e) => {
                foreach (Border b in ColorPickerList.Items) b.BorderThickness = new Thickness(0);
                border.BorderThickness = new Thickness(2);
                SelectedColorHex = (string)border.Tag;
            };
            ColorPickerList.Items.Add(border);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Basic Validation
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                AlertDialog.Show("Vui lòng nhập tên ca làm việc.", "Thông báo", AlertType.Warning, this);
                return;
            }

            if (!TimeOnly.TryParse(StartTimeTextBox.Text, out var start) || 
                !TimeOnly.TryParse(EndTimeTextBox.Text, out var end) ||
                !TimeOnly.TryParse(BreakStartTextBox.Text, out var bStart) ||
                !TimeOnly.TryParse(BreakEndTextBox.Text, out var bEnd))
            {
                AlertDialog.Show("Định dạng thời gian không hợp lệ (HH:mm). Vui lòng kiểm tra lại.", "Lỗi định dạng", AlertType.Error, this);
                return;
            }

            // Logical Validation
            if (start >= end)
            {
                AlertDialog.Show("Giờ bắt đầu phải nhỏ hơn giờ kết thúc.", "Lỗi logic", AlertType.Warning, this);
                return;
            }

            Shift.ShiftName = NameTextBox.Text;
            Shift.StartTime = start;
            Shift.EndTime = end;
            Shift.BreakStartTime = bStart;
            Shift.BreakEndTime = bEnd;
            Shift.ColorHex = SelectedColorHex;
            Shift.IsActive = IsActiveCheckBox.IsChecked ?? true;

            DialogResult = true;
            this.Close();
        }
        catch (Exception ex)
        {
            AlertDialog.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi hệ thống", AlertType.Error, this);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        this.Close();
    }
}
