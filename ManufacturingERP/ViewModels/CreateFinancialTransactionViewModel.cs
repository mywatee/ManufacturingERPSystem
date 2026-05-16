using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;

public partial class CreateFinancialTransactionViewModel : ViewModelBase
{
    private readonly IFinanceService _financeService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private DateTime _date = DateTime.Now;
    [ObservableProperty] private string _type = "Chi"; // Thu or Chi
    [ObservableProperty] private string _category = "Khác";
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _reference = "";
    [ObservableProperty] private string _method = "Chuyển khoản";
    [ObservableProperty] private bool _isOverhead;
    [ObservableProperty] private bool _isReadOnly;
    [ObservableProperty] private string _titleText = "Tạo phiếu thu / chi";
    [ObservableProperty] private string _subtitleText = "Ghi nhận dòng tiền thu vào hoặc chi ra thực tế";

    public ObservableCollection<string> TransactionTypes { get; } = new() { "Thu", "Chi" };
    public ObservableCollection<string> Categories { get; } = new() { "Lương", "Vật tư", "Bán hàng", "Tiện ích", "Cố định", "Khác" };
    public ObservableCollection<string> PaymentMethods { get; } = new() { "Chuyển khoản", "Tiền mặt", "Thẻ" };

    public CreateFinancialTransactionViewModel(
        IFinanceService financeService,
        INotificationService notificationService)
    {
        _financeService = financeService;
        _notificationService = notificationService;
    }

    public override Task InitializeAsync() 
    {
        IsReadOnly = false;
        TitleText = "Tạo phiếu thu / chi";
        SubtitleText = "Ghi nhận dòng tiền thu vào hoặc chi ra thực tế";
        return Task.CompletedTask;
    }

    public async Task InitializeForViewAsync(FinancialTransaction transaction)
    {
        if (transaction == null) return;

        Date = transaction.Date;
        Type = transaction.Type;
        Category = transaction.Category;
        Amount = transaction.Amount;
        Description = transaction.Description;
        Reference = transaction.Reference ?? "";
        Method = transaction.Method;
        IsOverhead = transaction.IsOverhead;
        
        IsReadOnly = true;
        TitleText = "Chi tiết phiếu thu / chi";
        SubtitleText = $"Thông tin chi tiết giao dịch {transaction.TransactionCode}";
    }

    [RelayCommand]
    private async Task SaveAsync(Window window)
    {
        if (IsReadOnly) return;
        if (Amount <= 0)
        {
            _notificationService.ShowError("Số tiền phải lớn hơn 0.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            _notificationService.ShowError("Vui lòng nhập mô tả.");
            return;
        }

        try
        {
            var transaction = new FinancialTransaction
            {
                Date = Date,
                Type = Type,
                Category = Category,
                Amount = Amount,
                Description = Description,
                Reference = Reference,
                Method = Method,
                IsOverhead = IsOverhead
            };

            var success = await _financeService.AddTransactionAsync(transaction);
            if (success)
            {
                _notificationService.ShowSuccess("Đã tạo phiếu thu/chi thành công.");
                window.DialogResult = true;
                window.Close();
            }
            else
            {
                _notificationService.ShowError("Lỗi khi lưu giao dịch.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi: " + ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
