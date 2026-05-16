using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ManufacturingERP.Views.Dialogs;

public partial class RoutingStepDialog : Window
{
    public class RoutingStepEntry
    {
        public int StepNo { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string WorkCenter { get; set; } = string.Empty;
        public double StdTimeMinutes { get; set; } = 0;
        public string Output { get; set; } = string.Empty;
    }

    public ObservableCollection<RoutingStepEntry> Steps { get; } = new();

    public RoutingStepDialog(IEnumerable<RoutingStepEntry>? existingSteps, int suggestedStepNo)
    {
        InitializeComponent();
        
        this.MouseDown += (s, e) => 
        { 
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) 
                DragMove(); 
        };

        // Load existing steps if any
        if (existingSteps != null)
        {
            foreach (var step in existingSteps)
            {
                Steps.Add(new RoutingStepEntry
                {
                    StepNo = step.StepNo,
                    StepName = step.StepName,
                    WorkCenter = step.WorkCenter,
                    StdTimeMinutes = step.StdTimeMinutes,
                    Output = step.Output
                });
            }
        }

        // Add next suggested row
        Steps.Add(new RoutingStepEntry { StepNo = suggestedStepNo });
        GridSteps.ItemsSource = Steps;
    }

    private void GridSteps_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
        var lastStep = Steps.LastOrDefault();
        var nextNo = lastStep == null ? 10 : lastStep.StepNo + 10;
        e.NewItem = new RoutingStepEntry { StepNo = nextNo };
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is RoutingStepEntry entry)
        {
            Steps.Remove(entry);
        }
    }

    private System.Windows.Threading.DispatcherTimer? _errorTimer;

    private void ShowErrorMessage(string message)
    {
        TxtError.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
        TipBorder.Visibility = Visibility.Collapsed;

        // Reset and start timer to auto-hide
        _errorTimer?.Stop();
        _errorTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(6) };
        _errorTimer.Tick += (s, e) => { HideError(); _errorTimer.Stop(); };
        _errorTimer.Start();
    }

    private void HideError()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
        TipBorder.Visibility = Visibility.Visible;
    }

    private void DismissError_Click(object sender, RoutedEventArgs e) => HideError();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // 1. Lọc bỏ các dòng hoàn toàn trống
        var validSteps = Steps.Where(s => !string.IsNullOrWhiteSpace(s.StepName) || !string.IsNullOrWhiteSpace(s.WorkCenter)).ToList();

        if (!validSteps.Any())
        {
            ShowErrorMessage("Vui lòng nhập ít nhất một công đoạn hợp lệ.");
            return;
        }

        foreach (var s in validSteps)
        {
            // 2. Kiểm tra Tên công đoạn
            if (string.IsNullOrWhiteSpace(s.StepName))
            {
                ShowErrorMessage($"Dòng STT {s.StepNo}: Tên bước sản xuất không được để trống.");
                return;
            }
            if (s.StepName.Length > 200)
            {
                ShowErrorMessage($"Dòng STT {s.StepNo}: Tên bước quá dài (tối đa 200 ký tự).");
                return;
            }

            // 3. Kiểm tra Tổ / Trung tâm sản xuất (Bắt buộc để quản lý)
            if (string.IsNullOrWhiteSpace(s.WorkCenter))
            {
                ShowErrorMessage($"Dòng STT {s.StepNo}: Vui lòng nhập Tổ / Thiết bị thực hiện.");
                return;
            }

            // 4. Kiểm tra Số thứ tự
            if (s.StepNo <= 0)
            {
                ShowErrorMessage($"Dòng STT {s.StepNo}: Số thứ tự công đoạn phải là số dương.");
                return;
            }

            // 5. Kiểm tra Thời gian định mức (Phải > 0 để tính giá thành)
            if (s.StdTimeMinutes <= 0)
            {
                ShowErrorMessage($"Dòng '{s.StepName}': Thời gian định mức phải lớn hơn 0.");
                return;
            }
        }

        // 6. Kiểm tra trùng lặp STT
        if (validSteps.GroupBy(s => s.StepNo).Any(g => g.Count() > 1))
        {
            ShowErrorMessage("Số thứ tự (STT) công đoạn không được trùng lặp.");
            return;
        }

        // Cập nhật lại danh sách sạch sẽ
        Steps.Clear();
        foreach(var s in validSteps.OrderBy(x => x.StepNo)) Steps.Add(s);

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
