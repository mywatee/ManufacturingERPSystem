using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ManufacturingERP.Core;
using ManufacturingERP.Models;
using ManufacturingERP.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using ManufacturingERP.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;


namespace ManufacturingERP.ViewModels;

public partial class ColumnOption : ObservableObject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    [ObservableProperty] private bool _isSelected = true;
}

public partial class FinanceViewModel : ViewModelBase
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IFinanceService _financeService;
    private readonly INotificationService _notificationService;
    private readonly IPartnerService _partnerService;
    private readonly INavigationService _navigationService;
    private readonly IFileService _fileService;
    private readonly IAccessControlService _accessControlService;

    // Debt Management Data
    [ObservableProperty] private ObservableCollection<Invoice> _accountsPayable = new();
    [ObservableProperty] private ObservableCollection<Invoice> _accountsReceivable = new();

    // Cash Flow Data
    [ObservableProperty] private ObservableCollection<FinancialTransaction> _transactions = new();

    // Cost Analysis Data
    [ObservableProperty] private ObservableCollection<CostDetailItem> _productCosts = new();
    [ObservableProperty] private ObservableCollection<ProfitAnalysisItem> _profitAnalysis = new();

    private List<Invoice> _allAP = new();
    private List<Invoice> _allAR = new();
    private List<FinancialTransaction> _allTX = new();

    // KPIs - Debt
    [ObservableProperty] private string _totalPayable = "0 đ";
    [ObservableProperty] private string _overduePayable = "0 đ";
    [ObservableProperty] private string _totalReceivable = "0 đ";
    [ObservableProperty] private string _overdueReceivable = "0 đ";
    [ObservableProperty] private string _aPCountText = "0 hóa đơn";
    [ObservableProperty] private string _aRCountText = "0 hóa đơn";

    // KPIs - Cash Flow
    [ObservableProperty] private string _totalInflow = "0 đ";
    [ObservableProperty] private string _totalOutflow = "0 đ";

    [ObservableProperty] private string _searchTextAP = "";
    [ObservableProperty] private string _searchTextAR = "";
    [ObservableProperty] private string _searchTextTX = "";
    [ObservableProperty] private DateTime? _startDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime? _endDate = DateTime.Now;

    // Cost Analysis Specific Filters
    [ObservableProperty] private DateTime? _costStartDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime? _costEndDate = DateTime.Now;

    // Profit Analysis Specific Filters
    [ObservableProperty] private DateTime? _profitStartDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime? _profitEndDate = DateTime.Now;

    // Chart Specific Filters
    [ObservableProperty] private DateTime? _chartStartDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime? _chartEndDate = DateTime.Now;

    [ObservableProperty] private string _netCashFlow = "0 đ";
    
    // Status Filters
    public List<string> InvoiceStatuses { get; } = new() { "Tất cả", "Chưa thanh toán", "Đã thanh toán", "Quá hạn" };
    [ObservableProperty] private string _selectedStatusAP = "Tất cả";
    [ObservableProperty] private string _selectedStatusAR = "Tất cả";
    
    // Pagination AP
    [ObservableProperty] private int _currentPageAP = 1;
    [ObservableProperty] private int _totalPagesAP = 1;
    [ObservableProperty] private int _pageSize = 20;

    // Pagination AR
    [ObservableProperty] private int _currentPageAR = 1;
    [ObservableProperty] private int _totalPagesAR = 1;

    // KPIs - Cost Analysis
    [ObservableProperty] private string _totalRevenueText = "0 đ";
    [ObservableProperty] private string _totalCostText = "0 đ";
    [ObservableProperty] private string _profitText = "0 đ";
    [ObservableProperty] private string _avgMarginText = "0%";

    // Pagination TX
    [ObservableProperty] private int _currentPageTX = 1;
    [ObservableProperty] private int _totalPagesTX = 1;
    [ObservableProperty] private int _pageSizeTX = 15;
    [ObservableProperty] private bool _canNextTX;
    [ObservableProperty] private bool _canPrevTX;

    // Pagination State
    [ObservableProperty] private bool _canNextAP;
    [ObservableProperty] private bool _canPrevAP;
    [ObservableProperty] private bool _canNextAR;
    [ObservableProperty] private bool _canPrevAR;

    // Pagination - Cost Analysis
    [ObservableProperty] private int _currentPageCost = 1;
    [ObservableProperty] private int _totalPagesCost = 1;
    [ObservableProperty] private int _pageSizeCost = 15;
    [ObservableProperty] private bool _canNextCost;
    [ObservableProperty] private bool _canPrevCost;

    // Pagination - Profit Analysis
    [ObservableProperty] private int _currentPageProfit = 1;
    [ObservableProperty] private int _totalPagesProfit = 1;
    [ObservableProperty] private int _pageSizeProfit = 15;
    [ObservableProperty] private bool _canNextProfit;
    [ObservableProperty] private bool _canPrevProfit;

    private List<CostDetailItem> _allProductCosts = new();
    private List<ProfitAnalysisItem> _allProfitAnalysis = new();

    // Report Configuration
    [ObservableProperty] private bool _isCostReportSelected = true;
    [ObservableProperty] private bool _isFinancialReportSelected;
    [ObservableProperty] private DateTime? _reportStartDate = DateTime.Now.AddMonths(-1);
    [ObservableProperty] private DateTime? _reportEndDate = DateTime.Now;
    [ObservableProperty] private bool _isExcelReport = true;
    [ObservableProperty] private bool _isPdfReport;
    [ObservableProperty] private bool _includeChartsInReport = true;
    [ObservableProperty] private bool _includeTransactionDetails = true;
    [ObservableProperty] private bool _summaryByCategory = true;
    
    // Type specific options
    [ObservableProperty] private bool _showWorkOrderDetails = true;
    [ObservableProperty] private bool _showOverdueDebt = true;
    [ObservableProperty] private bool _summaryByPartner = true;

    [ObservableProperty] private ObservableCollection<ColumnOption> _exportColumns = new();

    // Overhead Allocation Metadata
    [ObservableProperty] private string _totalMonthlyOverhead = "0 đ";
    [ObservableProperty] private string _totalMonthlyLaborHours = "0h";
    [ObservableProperty] private string _overheadRatePerHourText = "0 đ/h";

    // LiveCharts Properties
    [ObservableProperty] private SeriesCollection _cashFlowSeries = new();
    [ObservableProperty] private string[] _cashFlowLabels = Array.Empty<string>();
    [ObservableProperty] private Func<double, string> _yAxisFormatter = value => (value / 1000000).ToString("N1") + "M";

    [ObservableProperty] private SeriesCollection _profitByGroupSeries = new();
    [ObservableProperty] private SeriesCollection _profitByCustomerSeries = new();
    [ObservableProperty] private string[] _customerLabels = Array.Empty<string>();

    // Chart Filters
    public List<string> ChartPeriods { get; } = new() { "3 tháng gần nhất", "6 tháng gần nhất", "12 tháng gần nhất", "Năm nay" };
    [ObservableProperty] private string _selectedChartPeriod = "6 tháng gần nhất";

    public List<string> ProductGroups { get; private set; } = new() { "Tất cả nhóm" };
    [ObservableProperty] private string _selectedProductGroup = "Tất cả nhóm";

    [ObservableProperty] private bool _canAddFinance;
    [ObservableProperty] private bool _canEditFinance;
    [ObservableProperty] private bool _canDeleteFinance;

    public FinanceViewModel(
        IDbContextFactory<ManufacturingContext> contextFactory, 
        IFinanceService financeService,
        INotificationService notificationService,
        IPartnerService partnerService,
        INavigationService navigationService,
        IFileService fileService,
        IAccessControlService accessControlService)
    {
        _contextFactory = contextFactory;
        _financeService = financeService;
        _notificationService = notificationService;
        _partnerService = partnerService;
        _navigationService = navigationService;
        _fileService = fileService;
        _accessControlService = accessControlService;

        // Subscribe to refresh message
        WeakReferenceMessenger.Default.Register<InvoiceCreatedMessage>(this, (r, m) =>
        {
            _ = LoadDataAsync();
        });

        UpdateExportColumns();
    }

    partial void OnIsCostReportSelectedChanged(bool value) => UpdateExportColumns();
    partial void OnIsFinancialReportSelectedChanged(bool value) => UpdateExportColumns();

    private void UpdateExportColumns()
    {
        ExportColumns.Clear();
        if (IsCostReportSelected)
        {
            ExportColumns.Add(new ColumnOption { Id = "MaSP", Name = "Mã sản phẩm" });
            ExportColumns.Add(new ColumnOption { Id = "TenSP", Name = "Tên sản phẩm" });
            ExportColumns.Add(new ColumnOption { Id = "Nhom", Name = "Nhóm sản phẩm" });
            ExportColumns.Add(new ColumnOption { Id = "SoLuong", Name = "Số lượng" });
            ExportColumns.Add(new ColumnOption { Id = "DonGia", Name = "Đơn giá" });
            ExportColumns.Add(new ColumnOption { Id = "GiaThanh", Name = "Tổng giá thành" });
            ExportColumns.Add(new ColumnOption { Id = "CP_NguyenLieu", Name = "Chi phí Nguyên vật liệu" });
            ExportColumns.Add(new ColumnOption { Id = "CP_NhanCong", Name = "Chi phí Nhân công" });
            ExportColumns.Add(new ColumnOption { Id = "CP_SanXuatChung", Name = "Chi phí Sản xuất chung" });
        }
        else
        {
            ExportColumns.Add(new ColumnOption { Id = "Ngay", Name = "Ngày giao dịch" });
            ExportColumns.Add(new ColumnOption { Id = "MaGD", Name = "Mã giao dịch" });
            ExportColumns.Add(new ColumnOption { Id = "Loai", Name = "Loại giao dịch" });
            ExportColumns.Add(new ColumnOption { Id = "SoTien", Name = "Số tiền" });
            ExportColumns.Add(new ColumnOption { Id = "HangMuc", Name = "Hạng mục" });
            ExportColumns.Add(new ColumnOption { Id = "MoTa", Name = "Mô tả" });
            ExportColumns.Add(new ColumnOption { Id = "PhuongThuc", Name = "Phương thức" });
        }
    }

    public override async Task InitializeAsync()
    {
        await _accessControlService.RefreshAsync();
        CanAddFinance = _accessControlService.HasCached(SystemModules.Finance, PermissionAction.Add);
        CanEditFinance = _accessControlService.HasCached(SystemModules.Finance, PermissionAction.Edit);
        CanDeleteFinance = _accessControlService.HasCached(SystemModules.Finance, PermissionAction.Delete);
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try 
        {
            _allAP = (await _financeService.GetInvoicesAsync("AP")).ToList();
            _allAR = (await _financeService.GetInvoicesAsync("AR")).ToList();
            _allTX = (await _financeService.GetTransactionsAsync()).ToList();

            var now = DateTime.Now;
            var flow = await _financeService.GetMonthlyCashFlowAsync(now.Month, now.Year);
            TotalInflow = (flow.Inflow / 1000000m).ToString("N1") + "M";
            TotalOutflow = (flow.Outflow / 1000000m).ToString("N1") + "M";
            NetCashFlow = ((flow.Inflow - flow.Outflow) / 1000000m).ToString("N1") + "M";
            ApplyFilters();
            
            await RefreshMonthlyOverheadAsync();
            await RefreshCostAnalysisAsync();
            await RefreshProfitAnalysisAsync();
            await RefreshChartsDataAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải dữ liệu tài chính: " + ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshChartsDataAsync()
    {
        await UpdateChartsAsync();
    }

    [RelayCommand]
    public async Task RefreshMonthlyOverheadAsync()
    {
        try
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            
            var overheadResult = await _financeService.CalculateProductionCostsAsync(startOfMonth, endOfMonth);
            
            // Update Overhead KPIs (Always fixed to this month)
            TotalMonthlyOverhead = overheadResult.TotalOverhead.ToString("N0") + " đ";
            TotalMonthlyLaborHours = overheadResult.TotalLaborHours.ToString("N1") + " giờ";
            OverheadRatePerHourText = overheadResult.OverheadRatePerHour.ToString("N0") + " đ/giờ";
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải thông tin chi phí chung: " + ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshCostAnalysisAsync()
    {
        try 
        {
            var costResult = await _financeService.CalculateProductionCostsAsync(CostStartDate, CostEndDate);
            _allProductCosts = costResult.Items;
            
            ApplyCostPagination();
            
            // We no longer update Overhead KPIs here, as they are fixed to the month (Option B)
            TotalCostText = (_allProductCosts.Sum(c => c.TotalCost) / 1000000m).ToString("N1") + "M";
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải dữ liệu giá thành: " + ex.Message);
        }
    }

    private void ApplyCostPagination()
    {
        TotalPagesCost = (int)Math.Ceiling(_allProductCosts.Count / (double)PageSizeCost);
        if (TotalPagesCost == 0) TotalPagesCost = 1;
        if (CurrentPageCost > TotalPagesCost) CurrentPageCost = TotalPagesCost;
        if (CurrentPageCost < 1) CurrentPageCost = 1;

        var pagedData = _allProductCosts.Skip((CurrentPageCost - 1) * PageSizeCost).Take(PageSizeCost).ToList();
        
        App.Current.Dispatcher.Invoke(() => {
            ProductCosts.Clear();
            foreach (var item in pagedData) ProductCosts.Add(item);
            
            CanNextCost = CurrentPageCost < TotalPagesCost;
            CanPrevCost = CurrentPageCost > 1;
        });
    }

    [RelayCommand]
    public async Task RefreshProfitAnalysisAsync()
    {
        try 
        {
            var costResult = await _financeService.CalculateProductionCostsAsync(ProfitStartDate, ProfitEndDate);
            
            _allProfitAnalysis = costResult.Items.Select(c => new ProfitAnalysisItem {
                Name = c.Name,
                ProductCategory = c.Category,
                Cost = c.TotalCost,
                Price = c.UnitPrice * 1.35m,
                Revenue = (c.UnitPrice * 1.35m) * c.Quantity,
                Profit = (c.UnitPrice * 0.35m) * c.Quantity,
                Margin = (c.UnitPrice > 0) ? (double)((c.UnitPrice * 0.35m) / (c.UnitPrice * 1.35m) * 100) : 0,
                Category = c.TotalCost > 0 ? ( (c.UnitPrice * 0.35m) / (c.UnitPrice * 1.35m) > 0.25m ? "Cao" : "Trung bình") : "N/A",
                MaterialRatio = (int)(c.MaterialCost * 100 / (c.TotalCost > 0 ? c.TotalCost : 1)),
                LaborRatio = (int)(c.LaborCost * 100 / (c.TotalCost > 0 ? c.TotalCost : 1)),
                OverheadRatio = (int)(c.OverheadCost * 100 / (c.TotalCost > 0 ? c.TotalCost : 1))
            }).ToList();
            
            ApplyProfitPagination();

            // Update Dynamic Product Groups
            var groups = _allProfitAnalysis.Select(a => a.ProductCategory).Distinct().OrderBy(g => g).ToList();
            var newGroups = new List<string> { "Tất cả nhóm" };
            newGroups.AddRange(groups);
            ProductGroups = newGroups;
            OnPropertyChanged(nameof(ProductGroups));

            TotalRevenueText = (_allProfitAnalysis.Sum(a => a.Revenue) / 1000000m).ToString("N1") + "M";
            ProfitText = (_allProfitAnalysis.Sum(a => a.Profit) / 1000000m).ToString("N1") + "M";
            AvgMarginText = _allProfitAnalysis.Any() ? _allProfitAnalysis.Average(a => a.Margin).ToString("N1") + "%" : "0%";
            
            await UpdateChartsAsync();
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi tải dữ liệu lợi nhuận: " + ex.Message);
        }
    }

    private void ApplyProfitPagination()
    {
        TotalPagesProfit = (int)Math.Ceiling(_allProfitAnalysis.Count / (double)PageSizeProfit);
        if (TotalPagesProfit == 0) TotalPagesProfit = 1;
        if (CurrentPageProfit > TotalPagesProfit) CurrentPageProfit = TotalPagesProfit;
        if (CurrentPageProfit < 1) CurrentPageProfit = 1;

        var pagedData = _allProfitAnalysis.Skip((CurrentPageProfit - 1) * PageSizeProfit).Take(PageSizeProfit).ToList();
        
        App.Current.Dispatcher.Invoke(() => {
            ProfitAnalysis.Clear();
            foreach (var item in pagedData) ProfitAnalysis.Add(item);
            
            CanNextProfit = CurrentPageProfit < TotalPagesProfit;
            CanPrevProfit = CurrentPageProfit > 1;
        });
    }

    partial void OnSelectedChartPeriodChanged(string value) { _ = UpdateChartsAsync(); }
    partial void OnSelectedProductGroupChanged(string value) { _ = UpdateChartsAsync(); }

    private async Task UpdateChartsAsync()
    {
        try
        {
            // 1. Monthly Cash Flow Chart (Remains based on SelectedChartPeriod dropdown)
            int monthsToLookBack = SelectedChartPeriod switch
            {
                "3 tháng gần nhất" => 3,
                "6 tháng gần nhất" => 6,
                "12 tháng gần nhất" => 12,
                "Năm nay" => DateTime.Now.Month,
                _ => 6
            };

            var months = new List<string>();
            var inflowData = new ChartValues<decimal>();
            var outflowData = new ChartValues<decimal>();

            for (int i = monthsToLookBack - 1; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var flow = await _financeService.GetMonthlyCashFlowAsync(date.Month, date.Year);
                months.Add(date.ToString("MM/yyyy"));
                inflowData.Add(flow.Inflow);
                outflowData.Add(flow.Outflow);
            }

            CashFlowLabels = months.ToArray();
            CashFlowSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Thu",
                    Values = inflowData,
                    Fill = System.Windows.Media.Brushes.MediumSeaGreen,
                    DataLabels = monthsToLookBack <= 6
                },
                new ColumnSeries
                {
                    Title = "Chi",
                    Values = outflowData,
                    Fill = System.Windows.Media.Brushes.IndianRed,
                    DataLabels = monthsToLookBack <= 6
                }
            };

            // 2. Profit by Product Group (Based on Chart Dates)
            var costResult = await _financeService.CalculateProductionCostsAsync(ChartStartDate, ChartEndDate);
            var chartProfitData = costResult.Items.Select(c => new ProfitAnalysisItem {
                Name = c.Name,
                ProductCategory = c.Category,
                Profit = (c.UnitPrice * 0.35m) * c.Quantity
            }).ToList();

            var filteredChartData = SelectedProductGroup == "Tất cả nhóm" 
                ? chartProfitData 
                : chartProfitData.Where(a => a.ProductCategory == SelectedProductGroup);

            if (SelectedProductGroup == "Tất cả nhóm")
            {
                var groupData = filteredChartData
                    .GroupBy(a => a.ProductCategory)
                    .Select(g => new { Category = g.Key, TotalProfit = g.Sum(a => a.Profit) })
                    .OrderByDescending(g => g.TotalProfit)
                    .ToList();

                var series = new SeriesCollection();
                foreach(var g in groupData) {
                    series.Add(new PieSeries { 
                        Title = g.Category, 
                        Values = new ChartValues<decimal> { g.TotalProfit }, 
                        DataLabels = true 
                    });
                }
                ProfitByGroupSeries = series;
            }
            else
            {
                var topProducts = filteredChartData
                    .OrderByDescending(a => a.Profit)
                    .Take(5)
                    .ToList();

                var series = new SeriesCollection();
                foreach(var p in topProducts) {
                    series.Add(new PieSeries { 
                        Title = p.Name, 
                        Values = new ChartValues<decimal> { p.Profit }, 
                        DataLabels = true 
                    });
                }
                ProfitByGroupSeries = series;
            }

            // 3. Profit by Strategic Customer (Based on Chart Dates)
            var customerProfit = _allAR
                .Where(i => i.Partner != null && 
                           (!ChartStartDate.HasValue || i.IssueDate.Date >= ChartStartDate.Value.Date) &&
                           (!ChartEndDate.HasValue || i.IssueDate.Date <= ChartEndDate.Value.Date))
                .GroupBy(i => i.Partner.PartnerName)
                .Select(g => new { 
                    Name = g.Key, 
                    EstimatedProfit = g.Sum(i => i.TotalAmount) * 0.35m 
                })
                .OrderByDescending(g => g.EstimatedProfit)
                .Take(5)
                .ToList();

            ProfitByCustomerSeries = new SeriesCollection
            {
                new RowSeries
                {
                    Title = "Lợi nhuận ước tính (VND)",
                    Values = new ChartValues<decimal>(customerProfit.Select(c => c.EstimatedProfit)),
                    DataLabels = true,
                    Fill = System.Windows.Media.Brushes.DodgerBlue
                }
            };
            CustomerLabels = customerProfit.Select(c => c.Name).ToArray();
        }
        catch (Exception ex)
        {
            // Silently fail chart updates or log to debug
            System.Diagnostics.Debug.WriteLine("Error updating charts: " + ex.Message);
        }
    }

    partial void OnSearchTextAPChanged(string value) { CurrentPageAP = 1; ApplyFilters(); }
    partial void OnSearchTextARChanged(string value) { CurrentPageAR = 1; ApplyFilters(); }
    partial void OnSearchTextTXChanged(string value) { CurrentPageTX = 1; ApplyFilters(); }
    partial void OnStartDateChanged(DateTime? value) { CurrentPageAP = 1; CurrentPageAR = 1; CurrentPageTX = 1; ApplyFilters(); }
    partial void OnEndDateChanged(DateTime? value) { CurrentPageAP = 1; CurrentPageAR = 1; CurrentPageTX = 1; ApplyFilters(); }
    
    partial void OnCostStartDateChanged(DateTime? value) { _ = RefreshCostAnalysisAsync(); }
    partial void OnCostEndDateChanged(DateTime? value) { _ = RefreshCostAnalysisAsync(); }
    
    partial void OnProfitStartDateChanged(DateTime? value) { _ = RefreshProfitAnalysisAsync(); }
    partial void OnProfitEndDateChanged(DateTime? value) { _ = RefreshProfitAnalysisAsync(); }

    partial void OnChartStartDateChanged(DateTime? value) { _ = RefreshChartsDataAsync(); }
    partial void OnChartEndDateChanged(DateTime? value) { _ = RefreshChartsDataAsync(); }
    partial void OnSelectedStatusAPChanged(string value) { CurrentPageAP = 1; ApplyFilters(); }
    partial void OnSelectedStatusARChanged(string value) { CurrentPageAR = 1; ApplyFilters(); }

    private void ApplyFilters()
    {
        // Filter AP
        var filteredAP = _allAP.Where(i => 
            (string.IsNullOrWhiteSpace(SearchTextAP) || i.InvoiceCode.Contains(SearchTextAP, StringComparison.OrdinalIgnoreCase) || (i.Partner != null && i.Partner.PartnerName.Contains(SearchTextAP, StringComparison.OrdinalIgnoreCase))) &&
            (!StartDate.HasValue || i.IssueDate.Date >= StartDate.Value.Date) &&
            (!EndDate.HasValue || i.IssueDate.Date <= EndDate.Value.Date) &&
            (SelectedStatusAP == "Tất cả" || i.Status == SelectedStatusAP)
        ).ToList();

        // Filter AR
        var filteredAR = _allAR.Where(i => 
            (string.IsNullOrWhiteSpace(SearchTextAR) || i.InvoiceCode.Contains(SearchTextAR, StringComparison.OrdinalIgnoreCase) || (i.Partner != null && i.Partner.PartnerName.Contains(SearchTextAR, StringComparison.OrdinalIgnoreCase))) &&
            (!StartDate.HasValue || i.IssueDate.Date >= StartDate.Value.Date) &&
            (!EndDate.HasValue || i.IssueDate.Date <= EndDate.Value.Date) &&
            (SelectedStatusAR == "Tất cả" || i.Status == SelectedStatusAR)
        ).ToList();

        // Filter Transactions
        var filteredTX = _allTX.Where(t => 
            (string.IsNullOrWhiteSpace(SearchTextTX) || 
             t.TransactionCode.Contains(SearchTextTX, StringComparison.OrdinalIgnoreCase) || 
             t.Category.Contains(SearchTextTX, StringComparison.OrdinalIgnoreCase) || 
             t.Description.Contains(SearchTextTX, StringComparison.OrdinalIgnoreCase)) &&
            (!StartDate.HasValue || t.Date.Date >= StartDate.Value.Date) &&
            (!EndDate.HasValue || t.Date.Date <= EndDate.Value.Date)
        ).ToList();

        TotalPagesAP = (int)Math.Ceiling(filteredAP.Count / (double)PageSize);
        if (TotalPagesAP == 0) TotalPagesAP = 1;
        if (CurrentPageAP > TotalPagesAP) CurrentPageAP = TotalPagesAP;

        TotalPagesAR = (int)Math.Ceiling(filteredAR.Count / (double)PageSize);
        if (TotalPagesAR == 0) TotalPagesAR = 1;
        if (CurrentPageAR > TotalPagesAR) CurrentPageAR = TotalPagesAR;

        TotalPagesTX = (int)Math.Ceiling(filteredTX.Count / (double)PageSizeTX);
        if (TotalPagesTX == 0) TotalPagesTX = 1;
        if (CurrentPageTX > TotalPagesTX) CurrentPageTX = TotalPagesTX;

        var pagedAP = filteredAP.Skip((CurrentPageAP - 1) * PageSize).Take(PageSize).ToList();
        var pagedAR = filteredAR.Skip((CurrentPageAR - 1) * PageSize).Take(PageSize).ToList();
        var pagedTX = filteredTX.Skip((CurrentPageTX - 1) * PageSizeTX).Take(PageSizeTX).ToList();

        App.Current.Dispatcher.Invoke(() => {
            AccountsPayable.Clear();
            foreach (var i in pagedAP) AccountsPayable.Add(i);

            AccountsReceivable.Clear();
            foreach (var i in pagedAR) AccountsReceivable.Add(i);

            Transactions.Clear();
            foreach (var t in pagedTX) Transactions.Add(t);
            
            APCountText = $"{filteredAP.Count} hóa đơn";
            ARCountText = $"{filteredAR.Count} hóa đơn";
            
            CanNextAP = CurrentPageAP < TotalPagesAP;
            CanPrevAP = CurrentPageAP > 1;
            CanNextAR = CurrentPageAR < TotalPagesAR;
            CanPrevAR = CurrentPageAR > 1;
            CanNextTX = CurrentPageTX < TotalPagesTX;
            CanPrevTX = CurrentPageTX > 1;
            
            UpdateKPIs(filteredAP, filteredAR, filteredTX);
        });
    }

    private void UpdateKPIs(List<Invoice> filteredAP, List<Invoice> filteredAR, List<FinancialTransaction> filteredTX)
    {
        // Debt KPIs - Use filtered lists (full data across all pages)
        TotalPayable = (filteredAP.Sum(a => a.RemainingAmount) / 1000000m).ToString("N1") + "M";
        TotalReceivable = (filteredAR.Sum(a => a.RemainingAmount) / 1000000m).ToString("N1") + "M";
        OverduePayable = (filteredAP.Where(a => a.Status == "Quá hạn").Sum(a => a.RemainingAmount) / 1000000m).ToString("N1") + "M";
        OverdueReceivable = (filteredAR.Where(a => a.Status == "Quá hạn").Sum(a => a.RemainingAmount) / 1000000m).ToString("N1") + "M";
    }

    [RelayCommand]
    public void NextPageAP()
    {
        if (CurrentPageAP < TotalPagesAP)
        {
            CurrentPageAP++;
            ApplyFilters();
        }
    }

    [RelayCommand]
    public void PreviousPageAP()
    {
        if (CurrentPageAP > 1)
        {
            CurrentPageAP--;
            ApplyFilters();
        }
    }

    [RelayCommand]
    public void NextPageAR()
    {
        if (CurrentPageAR < TotalPagesAR)
        {
            CurrentPageAR++;
            ApplyFilters();
        }
    }

    [RelayCommand]
    public void PreviousPageAR()
    {
        if (CurrentPageAR > 1)
        {
            CurrentPageAR--;
            ApplyFilters();
        }
    }

    [RelayCommand]
    public void NextPageTX()
    {
        if (CurrentPageTX < TotalPagesTX)
        {
            CurrentPageTX++;
            ApplyFilters();
        }
    }

    [RelayCommand]
    public void PreviousPageTX()
    {
        if (CurrentPageTX > 1)
        {
            CurrentPageTX--;
            ApplyFilters();
        }
    }
    
    [RelayCommand]
    public void NextPageCost()
    {
        if (CurrentPageCost < TotalPagesCost)
        {
            CurrentPageCost++;
            ApplyCostPagination();
        }
    }

    [RelayCommand]
    public void PreviousPageCost()
    {
        if (CurrentPageCost > 1)
        {
            CurrentPageCost--;
            ApplyCostPagination();
        }
    }

    [RelayCommand]
    public void NextPageProfit()
    {
        if (CurrentPageProfit < TotalPagesProfit)
        {
            CurrentPageProfit++;
            ApplyProfitPagination();
        }
    }

    [RelayCommand]
    public void PreviousPageProfit()
    {
        if (CurrentPageProfit > 1)
        {
            CurrentPageProfit--;
            ApplyProfitPagination();
        }
    }
    
    [RelayCommand]
    public async Task ExportReportAsync()
    {
        try
        {
            // 1. Chuẩn bị dữ liệu dựa trên loại báo cáo
            object exportData;
            string reportTitle;
            string defaultFileName;
            List<string> columns = null;

            if (IsCostReportSelected)
            {
                var costData = await _financeService.CalculateProductionCostsAsync(ReportStartDate, ReportEndDate);
                exportData = costData.Items.Select(c => new {
                    MaSP = c.Id,
                    TenSP = c.Name,
                    Nhom = c.Category,
                    SoLuong = c.Quantity,
                    DonGia = c.UnitPrice,
                    GiaThanh = c.TotalCost,
                    CP_NguyenLieu = c.MaterialCost,
                    CP_NhanCong = c.LaborCost,
                    CP_SanXuatChung = c.OverheadCost
                }).ToList();
                reportTitle = "BÁO CÁO CHI TIẾT GIÁ THÀNH SẢN PHẨM";
                defaultFileName = $"BaoCaoGiaThanh_{DateTime.Now:yyyyMMdd}";
            }
            else
            {
                // Báo cáo Tài chính & Dòng tiền
                exportData = _allTX.Where(t => 
                    (!ReportStartDate.HasValue || t.Date >= ReportStartDate.Value) &&
                    (!ReportEndDate.HasValue || t.Date <= ReportEndDate.Value))
                    .Select(t => new {
                        Ngay = t.Date.ToString("dd/MM/yyyy"),
                        MaGD = t.TransactionCode,
                        Loai = t.Type,
                        SoTien = t.Amount,
                        HangMuc = t.Category,
                        MoTa = t.Description,
                        PhuongThuc = t.Method
                    }).ToList();
                reportTitle = "BÁO CÁO DÒNG TIỀN VÀ GIAO DỊCH TÀI CHÍNH";
                defaultFileName = $"BaoCaoTaiChinh_{DateTime.Now:yyyyMMdd}";
            }

            // 2. Lấy danh sách các cột được chọn
            var selectedColumnNames = ExportColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            if (!selectedColumnNames.Any())
            {
                _notificationService.ShowWarning("Vui lòng chọn ít nhất một cột để xuất báo cáo.");
                return;
            }

            // 3. Mở hộp thoại lưu file
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = IsExcelReport ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF Document (*.pdf)|*.pdf",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _notificationService.ShowInfo("Đang khởi tạo báo cáo... Vui lòng đợi.");
                
                bool success = false;
                if (IsExcelReport)
                {
                    // Sử dụng Reflection để gọi phương thức generic
                    var method = _fileService.GetType().GetMethod("ExportToExcelAsync")?.MakeGenericMethod(exportData.GetType().GetGenericArguments()[0]);
                    if (method != null)
                    {
                        var task = (Task<bool>)method.Invoke(_fileService, new object[] { exportData, saveFileDialog.FileName, "Báo cáo", selectedColumnNames, reportTitle });
                        success = await task;
                    }
                }
                else
                {
                    var method = _fileService.GetType().GetMethod("ExportToPdfAsync")?.MakeGenericMethod(exportData.GetType().GetGenericArguments()[0]);
                    if (method != null)
                    {
                        var task = (Task<bool>)method.Invoke(_fileService, new object[] { exportData, saveFileDialog.FileName, reportTitle, selectedColumnNames });
                        success = await task;
                    }
                }

                if (success)
                {
                    _notificationService.ShowSuccess($"Đã xuất báo cáo thành công tại: {saveFileDialog.FileName}");
                }
                else
                {
                    _notificationService.ShowError("Có lỗi xảy ra trong quá trình xuất báo cáo.");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi xuất báo cáo: " + ex.Message);
        }
    }

    [RelayCommand]
    public async Task CreateInvoice()
    {
        if (!_accessControlService.HasCached(SystemModules.Finance, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Tài chính.");
            return;
        }

        var vm = _navigationService.NavigateTo<CreateInvoiceViewModel>();
        await vm.InitializeAsync();
    }

    [RelayCommand]
    public async Task EditInvoiceAsync(Invoice invoice)
    {
        if (invoice == null) return;
        if (!_accessControlService.HasCached(SystemModules.Finance, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Tài chính.");
            return;
        }

        var vm = _navigationService.NavigateTo<CreateInvoiceViewModel>();
        await vm.InitializeForEditAsync(invoice);
    }

    [RelayCommand]
    public async Task ViewInvoiceAsync(Invoice invoice)
    {
        if (invoice == null) return;
        var vm = _navigationService.NavigateTo<CreateInvoiceViewModel>();
        await vm.InitializeForViewAsync(invoice);
    }

    [RelayCommand]
    public async Task CreateTransactionAsync()
    {
        if (!_accessControlService.HasCached(SystemModules.Finance, PermissionAction.Add))
        {
            _notificationService.ShowError("Bạn không có quyền Thêm trong phân hệ Tài chính.");
            return;
        }

        var vm = App.Current.Dispatcher.Invoke(() => ((App)App.Current).Services.GetRequiredService<CreateFinancialTransactionViewModel>());
        await vm.InitializeAsync();

        var dialog = App.Current.Dispatcher.Invoke(() => new CreateFinancialTransactionDialog(vm));
        dialog.Owner = App.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]

    public async Task PayInvoiceAsync(Invoice invoice)
    {
        if (invoice == null) return;
        if (!_accessControlService.HasCached(SystemModules.Finance, PermissionAction.Edit))
        {
            _notificationService.ShowError("Bạn không có quyền Sửa trong phân hệ Tài chính.");
            return;
        }

        try
        {
            var transaction = new FinancialTransaction
            {
                Date = DateTime.Now,
                Type = invoice.Type == "AP" ? "Chi" : "Thu",
                Amount = invoice.RemainingAmount,
                Category = invoice.Type == "AP" ? "Thanh toán NCC" : "Thu tiền KH",
                Description = $"Thanh toán hóa đơn {invoice.InvoiceCode}",
                Reference = invoice.InvoiceCode,
                Method = "Chuyển khoản"
            };

            var successTx = await _financeService.AddTransactionAsync(transaction);
            if (successTx)
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var dbInvoice = await context.Invoices.FindAsync(invoice.InvoiceId);
                if (dbInvoice != null)
                {
                    dbInvoice.PaidAmount += transaction.Amount;
                    if (dbInvoice.PaidAmount >= dbInvoice.TotalAmount)
                        dbInvoice.Status = "Đã thanh toán";
                    else
                        dbInvoice.Status = "Một phần";
                    await context.SaveChangesAsync();
                }

                _notificationService.ShowSuccess($"Đã thanh toán hóa đơn {invoice.InvoiceCode} thành công.");
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Lỗi khi thực hiện thanh toán: " + ex.Message);
        }
    }

    [RelayCommand]
    public async Task DeleteInvoiceAsync(Invoice invoice)
    {
        if (invoice == null) return;
        if (!_accessControlService.HasCached(SystemModules.Finance, PermissionAction.Delete))
        {
            _notificationService.ShowError("Bạn không có quyền Xóa trong phân hệ Tài chính.");
            return;
        }

        var message = $"CẢNH BÁO: Hóa đơn {invoice.InvoiceCode} sẽ bị xóa vĩnh viễn khỏi hệ thống.\nCác báo cáo tài chính liên quan sẽ bị thay đổi.\n\nBạn có thực sự muốn thực hiện không?";
        
        var dialog = new ConfirmDialog("Xác nhận xóa dữ liệu", message);
        dialog.Owner = App.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var success = await _financeService.DeleteInvoiceAsync(invoice.InvoiceId);
                if (success)
                {
                    _notificationService.ShowSuccess($"Đã xóa hóa đơn {invoice.InvoiceCode}.");
                    await LoadDataAsync();
                }
                else
                {
                    _notificationService.ShowError("Không thể xóa hóa đơn này.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi xóa: " + ex.Message);
            }
        }
    }

    [RelayCommand]
    public async Task ViewTransactionAsync(FinancialTransaction transaction)
    {
        if (transaction == null) return;
        
        var vm = App.Current.Dispatcher.Invoke(() => ((App)App.Current).Services.GetRequiredService<CreateFinancialTransactionViewModel>());
        await vm.InitializeForViewAsync(transaction);

        var dialog = App.Current.Dispatcher.Invoke(() => new CreateFinancialTransactionDialog(vm));
        dialog.Owner = App.Current.MainWindow;
        dialog.ShowDialog();
    }

    [RelayCommand]
    public async Task DeleteTransactionAsync(FinancialTransaction transaction)
    {
        if (transaction == null) return;
        if (!_accessControlService.HasCached(SystemModules.Finance, PermissionAction.Delete))
        {
            _notificationService.ShowError("Bạn không có quyền Xóa trong phân hệ Tài chính.");
            return;
        }

        var message = $"Bạn có chắc chắn muốn xóa giao dịch {transaction.TransactionCode}?\n" +
                      $"Số tiền: {transaction.Amount:N0} đ\nMô tả: {transaction.Description}";
        
        var dialog = new ConfirmDialog("Xác nhận xóa giao dịch", message);
        dialog.Owner = App.Current.MainWindow;

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var success = await _financeService.DeleteTransactionAsync(transaction.TransactionId);
                if (success)
                {
                    _notificationService.ShowSuccess($"Đã xóa giao dịch {transaction.TransactionCode}.");
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError("Lỗi khi xóa giao dịch: " + ex.Message);
            }
        }
    }
}


public class ProfitAnalysisItem
{
    public string Name { get; set; } = "";
    public string ProductCategory { get; set; } = "";
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
    public double Margin { get; set; }
    public string Category { get; set; } = "";
    public int MaterialRatio { get; set; }
    public int LaborRatio { get; set; }
    public int OverheadRatio { get; set; }
}


