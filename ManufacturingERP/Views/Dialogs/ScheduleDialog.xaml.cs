using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ManufacturingERP.Models;

namespace ManufacturingERP.Views.Dialogs;

public partial class ScheduleDialog : Window
{
    private List<SelectableEmployee> _employees;
    private System.ComponentModel.ICollectionView _employeeView;

    public List<EmployeeSchedule> Results { get; private set; } = new List<EmployeeSchedule>();
    public bool IsDeleteMode { get; private set; }

    public ScheduleDialog(List<User> employees, List<Shift> shifts, List<int>? preSelectedUserIds = null, EmployeeSchedule? existing = null, bool isDeleteMode = false)
    {
        InitializeComponent();
        IsDeleteMode = isDeleteMode;

        if (IsDeleteMode)
        {
            DialogTitle.Text = "HỦY LỊCH TRỰC";
            SaveButton.Content = "Xác nhận xóa";
            SaveButton.Style = (Style)Application.Current.Resources["DangerButtonStyle"];
            ShiftContainer.Visibility = Visibility.Collapsed;
            NoteContainer.Visibility = Visibility.Collapsed;
        }
        else if (existing != null)
        {
            DialogTitle.Text = "CẬP NHẬT LỊCH TRỰC";
            SaveButton.Content = "Cập nhật thay đổi";
        }

        _employees = employees.Select(u => new SelectableEmployee 
        { 
            User = u, 
            IsSelected = preSelectedUserIds?.Contains(u.UserId) ?? false 
        }).ToList();
        _employeeView = System.Windows.Data.CollectionViewSource.GetDefaultView(_employees);
        _employeeView.Filter = (obj) =>
        {
            var item = obj as SelectableEmployee;
            if (string.IsNullOrWhiteSpace(EmployeeSearchTextBox.Text)) return true;
            return item.User.FullName.Contains(EmployeeSearchTextBox.Text, StringComparison.OrdinalIgnoreCase) ||
                   item.User.Username.Contains(EmployeeSearchTextBox.Text, StringComparison.OrdinalIgnoreCase);
        };

        EmployeesListView.ItemsSource = _employeeView;
        ShiftComboBox.ItemsSource = shifts;

        // Set default dates
        StartDatePicker.SelectedDate = DateTime.Today;
        EndDatePicker.SelectedDate = DateTime.Today;

        if (existing != null)
        {
            var selected = _employees.FirstOrDefault(e => e.User.UserId == existing.UserId);
            if (selected != null) selected.IsSelected = true;

            StartDatePicker.SelectedDate = existing.WorkDate?.ToDateTime(TimeOnly.MinValue);
            EndDatePicker.SelectedDate = existing.WorkDate?.ToDateTime(TimeOnly.MinValue);
            ShiftComboBox.SelectedItem = shifts.FirstOrDefault(s => s.ShiftId == existing.ShiftId);
            NoteTextBox.Text = existing.MachineCode;
        }

        UpdateSummary();
    }

    private void EmployeeSearchTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        _employeeView.Refresh();
    }

    private void SelectAll_Checked(object sender, RoutedEventArgs e)
    {
        foreach (SelectableEmployee item in _employees)
            item.IsSelected = true;
        UpdateSummary();
    }

    private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
    {
        foreach (SelectableEmployee item in _employees)
            item.IsSelected = false;
        UpdateSummary();
    }

    private void EmployeeCheckBox_Click(object sender, RoutedEventArgs e)
    {
        UpdateSummary();
    }

    private void DatePicker_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        int selectedCount = _employees.Count(e => e.IsSelected);
        int daysCount = 0;

        if (StartDatePicker.SelectedDate != null && EndDatePicker.SelectedDate != null)
        {
            var start = StartDatePicker.SelectedDate.Value;
            var end = EndDatePicker.SelectedDate.Value;
            if (end >= start)
            {
                daysCount = (end - start).Days + 1;
            }
        }

        if (selectedCount == 0)
        {
            SummaryTextBlock.Text = "Chưa chọn nhân viên nào.";
        }
        else
        {
            int totalTurns = selectedCount * daysCount;
            SummaryTextBlock.Text = $"Đã chọn {selectedCount} nhân viên | {daysCount} ngày | Tổng cộng {totalTurns} lượt trực.";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var selectedEmployees = _employees.Where(emp => emp.IsSelected).ToList();
        if (!selectedEmployees.Any())
        {
            AlertDialog.Show("Vui lòng chọn ít nhất một nhân viên.", "Thông báo", AlertType.Warning, this);
            return;
        }

        if (!IsDeleteMode && ShiftComboBox.SelectedItem == null)
        {
            AlertDialog.Show("Vui lòng chọn ca làm việc.", "Thông báo", AlertType.Warning, this);
            return;
        }

        if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
        {
            AlertDialog.Show("Vui lòng chọn khoảng thời gian.", "Thông báo", AlertType.Warning, this);
            return;
        }

        if (EndDatePicker.SelectedDate < StartDatePicker.SelectedDate)
        {
            AlertDialog.Show("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", "Thông báo", AlertType.Warning, this);
            return;
        }

        var shift = (Shift)ShiftComboBox.SelectedItem;
        var startDate = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value);
        var endDate = DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value);

        Results.Clear();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            foreach (var emp in selectedEmployees)
            {
                Results.Add(new EmployeeSchedule
                {
                    UserId = emp.User.UserId,
                    ShiftId = IsDeleteMode ? 0 : shift?.ShiftId ?? 0,
                    WorkDate = date,
                    MachineCode = NoteTextBox.Text
                });
            }
        }

        DialogResult = true;
        this.Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        this.Close();
    }
}

public class SelectableEmployee : System.ComponentModel.INotifyPropertyChanged
{
    public User User { get; set; }
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
