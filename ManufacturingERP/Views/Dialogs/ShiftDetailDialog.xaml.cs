using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ManufacturingERP.Models;

namespace ManufacturingERP.Views.Dialogs;

public partial class ShiftDetailDialog : Window
{
    public ShiftDetailDialog(Shift shift, List<Employee> assignedEmployees)
    {
        InitializeComponent();
        
        // Set Data
        ShiftNameText.Text = shift.ShiftName;
        WorkingTimeText.Text = $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}";
        BreakTimeText.Text = $"{shift.BreakStartTime:hh\\:mm} - {shift.BreakEndTime:hh\\:mm}";
        
        if (!string.IsNullOrEmpty(shift.ColorHex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(shift.ColorHex);
                ShiftColorStop1.Color = color;
                
                // Darken for gradient stop 2
                var darkerColor = Color.FromArgb(color.A, (byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7));
                ShiftColorStop2.Color = darkerColor;
                
                ColorPreview.Background = new SolidColorBrush(color);
            }
            catch { }
        }

        if (shift.IsActive == false)
        {
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            StatusText.Text = "Đã dừng hoạt động";
        }

        EmployeeList.ItemsSource = assignedEmployees;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
