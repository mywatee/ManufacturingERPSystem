using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ManufacturingERP.Core;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;

namespace ManufacturingERP.ViewModels;

public partial class CreateInvoiceViewModel : ViewModelBase
{
    private readonly IFinanceService _financeService;
    private readonly IPartnerService _partnerService;
    private readonly INotificationService _notificationService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private string _invoiceCode = "";
    [ObservableProperty] private string _invoiceType = "AP"; // AP or AR
    [ObservableProperty] private Partner? _selectedPartner;
    [ObservableProperty] private DateTime _issueDate = DateTime.Now;
    [ObservableProperty] private DateTime _dueDate = DateTime.Now.AddDays(30);
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _vatRate = 10;
    [ObservableProperty] private decimal _vatAmount;
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private bool _isPaidImmediately;
    [ObservableProperty] private string _reference = "";
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int? _editInvoiceId;
    [ObservableProperty] private bool _isViewMode;
    [ObservableProperty] private string _pageTitle = "Tạo hóa đơn tài chính mới";
    [ObservableProperty] private string _saveButtonText = "Lưu hóa đơn";
    [ObservableProperty] private string _partnerNameInput = "";

    public ObservableCollection<Partner> Partners { get; } = new();
    public ObservableCollection<string> InvoiceTypes { get; } = new() { "AP", "AR" };
    public ObservableCollection<InvoiceItem> Items { get; } = new();

    public CreateInvoiceViewModel(
        IFinanceService financeService,
        IPartnerService partnerService,
        INotificationService notificationService,
        INavigationService navigationService)
    {
        _financeService = financeService;
        _partnerService = partnerService;
        _notificationService = notificationService;
        _navigationService = navigationService;
        
        GenerateInvoiceCode();
        Items.CollectionChanged += (s, e) => CalculateTotals();
    }

    public override async Task InitializeAsync()
    {
        // Reset fields
        GenerateInvoiceCode();
        InvoiceType = "AP";
        SelectedPartner = null;
        IssueDate = DateTime.Now;
        DueDate = DateTime.Now.AddDays(30);
        TotalAmount = 0;
        VatRate = 10;
        VatAmount = 0;
        SubTotal = 0;
        IsPaidImmediately = false;
        Reference = "";
        Note = "";
        IsViewMode = false;
        EditInvoiceId = null;
        SaveButtonText = "Lưu hóa đơn";
        PageTitle = "Tạo hóa đơn tài chính mới";
        Items.Clear();
        
        await LoadPartnersAsync();
    }

    public async Task InitializeForViewAsync(Invoice invoice)
    {
        await InitializeForEditAsync(invoice);
        IsViewMode = true;
        PageTitle = $"Chi tiết hóa đơn {invoice.InvoiceCode}";
    }

    public async Task InitializeForEditAsync(Invoice invoice)
    {
        await InitializeAsync();
        
        EditInvoiceId = invoice.InvoiceId;
        PageTitle = $"Chỉnh sửa hóa đơn {invoice.InvoiceCode}";
        SaveButtonText = "Cập nhật hóa đơn";
        InvoiceCode = invoice.InvoiceCode;
        InvoiceType = invoice.Type;
        SelectedPartner = Partners.FirstOrDefault(p => p.PartnerId == invoice.PartnerId);
        IssueDate = invoice.IssueDate;
        DueDate = invoice.DueDate;
        VatRate = invoice.VatRate;
        Reference = invoice.Reference ?? "";
        Note = invoice.Note;
        IsPaidImmediately = invoice.PaidAmount >= invoice.TotalAmount;

        Items.Clear();
        foreach (var item in invoice.Items)
        {
            item.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(InvoiceItem.Quantity) || e.PropertyName == nameof(InvoiceItem.UnitPrice))
                {
                    CalculateTotals();
                }
            };
            Items.Add(item);
        }
        CalculateTotals();
    }

    [RelayCommand]
    private void GenerateInvoiceCode()
    {
        InvoiceCode = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
    }

    [RelayCommand]
    private void AddItem()
    {
        var item = new InvoiceItem { ProductName = "Sản phẩm mới", Quantity = 1, UnitPrice = 0 };
        item.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(InvoiceItem.Quantity) || e.PropertyName == nameof(InvoiceItem.UnitPrice))
            {
                CalculateTotals();
                // Trigger refresh for TotalPrice in DataGrid
                OnPropertyChanged(nameof(Items));
            }
        };
        Items.Add(item);
    }

    [RelayCommand]
    private void RemoveItem(InvoiceItem item)
    {
        if (item != null) Items.Remove(item);
    }

    private void CalculateTotals()
    {
        SubTotal = Items.Sum(i => i.TotalPrice);
        VatAmount = SubTotal * (VatRate / 100);
        TotalAmount = SubTotal + VatAmount;
    }

    partial void OnVatRateChanged(decimal value) => CalculateTotals();

    private async Task LoadPartnersAsync()
    {
        try
        {
            var partners = await _partnerService.GetAllAsync();
            App.Current.Dispatcher.Invoke(() =>
            {
                Partners.Clear();
                foreach (var p in partners) Partners.Add(p);
            });
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải danh sách đối tác: " + ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsViewMode) return;
        
        if (string.IsNullOrWhiteSpace(InvoiceCode))
        {
            _notificationService.ShowError("Vui lòng nhập mã hóa đơn.");
            return;
        }

        if (SelectedPartner == null && string.IsNullOrWhiteSpace(PartnerNameInput))
        {
            _notificationService.ShowError("Vui lòng chọn hoặc nhập tên đối tác.");
            return;
        }

        if (TotalAmount <= 0)
        {
            _notificationService.ShowError("Số tiền phải lớn hơn 0. Vui lòng thêm mặt hàng.");
            return;
        }

        try
        {
            IsBusy = true;
            
            int partnerId = 0;
            if (SelectedPartner != null)
            {
                partnerId = SelectedPartner.PartnerId;
            }
            else
            {
                // Create guest partner
                var newPartner = new Partner
                {
                    PartnerName = PartnerNameInput,
                    PartnerCode = "GUEST-" + DateTime.Now.Ticks.ToString().Substring(10),
                    PartnerType = InvoiceType == "AP" ? "Supplier" : "Customer",
                    Status = "Hoạt động"
                };
                var successPartner = await _partnerService.AddAsync(newPartner);
                if (!successPartner)
                {
                    _notificationService.ShowError("Không thể tạo đối tác mới. Có thể do trùng tên hoặc lỗi hệ thống.");
                    IsBusy = false;
                    return;
                }
                partnerId = newPartner.PartnerId;
            }

            var invoice = new Invoice
            {
                InvoiceCode = InvoiceCode,
                Type = InvoiceType,
                PartnerId = partnerId,
                IssueDate = IssueDate,
                DueDate = DueDate,
                TotalAmount = TotalAmount,
                VatRate = VatRate,
                VatAmount = VatAmount,
                PaidAmount = IsPaidImmediately ? TotalAmount : 0,
                Status = IsPaidImmediately ? "Đã thanh toán" : "Chưa thanh toán",
                Reference = Reference,
                Note = Note,
                Items = Items.ToList()
            };

            bool success;
            if (EditInvoiceId.HasValue)
            {
                invoice.InvoiceId = EditInvoiceId.Value;
                success = await _financeService.UpdateInvoiceAsync(invoice);
            }
            else
            {
                success = await _financeService.AddInvoiceAsync(invoice);
            }
            if (success)
            {
                if (IsPaidImmediately)
                {
                    // Create transaction
                    var tx = new FinancialTransaction
                    {
                        Date = DateTime.Now,
                        Type = InvoiceType == "AP" ? "Chi" : "Thu",
                        Amount = TotalAmount,
                        Category = InvoiceType == "AP" ? "Thanh toán NCC" : "Thu tiền KH",
                        Description = $"Thanh toán ngay hóa đơn {InvoiceCode}",
                        Reference = InvoiceCode
                    };
                    await _financeService.AddTransactionAsync(tx);
                }

                WeakReferenceMessenger.Default.Send(new InvoiceCreatedMessage(InvoiceCode));
                _notificationService.ShowSuccess("Đã tạo hóa đơn thành công.");
                _navigationService.GoBack();
            }
            else
            {
                _notificationService.ShowError("Lỗi khi lưu hóa đơn.");
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
