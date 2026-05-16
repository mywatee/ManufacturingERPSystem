using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManufacturingERP.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingERP.Services;

public class FinanceService : IFinanceService
{
    private readonly IDbContextFactory<ManufacturingContext> _contextFactory;
    private readonly IAuditLogService _auditLogService;

    public FinanceService(IDbContextFactory<ManufacturingContext> contextFactory, IAuditLogService auditLogService)
    {
        _contextFactory = contextFactory;
        _auditLogService = auditLogService;
    }

    public async Task<List<FinancialTransaction>> GetTransactionsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.FinancialTransactions
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<List<Invoice>> GetInvoicesAsync(string type)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Invoices
            .Include(i => i.Partner)
            .Where(i => i.Type == type)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync();
    }

    public async Task<bool> AddTransactionAsync(FinancialTransaction transaction)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.FinancialTransactions.Add(transaction);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("TẠO GIAO DỊCH", $"Đã tạo phiếu {transaction.Type} mới: {transaction.Description} - Số tiền: {transaction.Amount:N0}", "FinancialTransactions");
        return success;
    }

    public async Task<bool> AddInvoiceAsync(Invoice invoice)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Invoices.Add(invoice);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("TẠO HÓA ĐƠN", $"Đã tạo hóa đơn {invoice.Type} mới: {invoice.InvoiceCode}", "Invoices");
        return success;
    }

    public async Task<bool> UpdateInvoiceAsync(Invoice invoice)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var dbInvoice = await context.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.InvoiceId == invoice.InvoiceId);
        if (dbInvoice == null) return false;

        context.Entry(dbInvoice).CurrentValues.SetValues(invoice);
        context.InvoiceItems.RemoveRange(dbInvoice.Items);
        dbInvoice.Items = invoice.Items;

        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("CẬP NHẬT HÓA ĐƠN", $"Đã cập nhật hóa đơn: {invoice.InvoiceCode}", "Invoices");
        return success;
    }

    public async Task<bool> DeleteInvoiceAsync(int invoiceId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var invoice = await context.Invoices.FindAsync(invoiceId);
        if (invoice == null) return false;

        context.Invoices.Remove(invoice);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("XÓA HÓA ĐƠN", $"Đã xóa hóa đơn: {invoice.InvoiceCode}", "Invoices");
        return success;
    }

    public async Task<bool> DeleteTransactionAsync(int transactionId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.FinancialTransactions.FindAsync(transactionId);
        if (transaction == null) return false;

        context.FinancialTransactions.Remove(transaction);
        bool success = await context.SaveChangesAsync() > 0;
        if (success) await _auditLogService.LogActionAsync("XÓA GIAO DỊCH", $"Đã xóa phiếu giao dịch ID {transactionId}", "FinancialTransactions");
        return success;
    }

    public async Task<(decimal Inflow, decimal Outflow)> GetMonthlyCashFlowAsync(int month, int year)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var inflow = await context.FinancialTransactions
            .Where(t => t.Date >= startDate && t.Date <= endDate && t.Type == "Thu")
            .SumAsync(t => t.Amount);

        var outflow = await context.FinancialTransactions
            .Where(t => t.Date >= startDate && t.Date <= endDate && t.Type == "Chi")
            .SumAsync(t => t.Amount);

        return (inflow, outflow);
    }

    public async Task<ProductionCostResult> CalculateProductionCostsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // 1. Calculate Monthly Overhead Rate
        var filterStart = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var filterEnd = endDate ?? DateTime.Now;
        
        // Categories considered as Manufacturing Overhead (Legacy Support)
        var overheadCategories = new[] { "Tiền điện", "Thuê xưởng", "Bảo trì máy", "Khấu hao máy", "Lương quản lý" };
        
        var totalOverheadInMonth = await context.FinancialTransactions
            .Where(t => t.Date >= filterStart && t.Date <= filterEnd && t.Type == "Chi" && (t.IsOverhead || overheadCategories.Contains(t.Category)))
            .SumAsync(t => t.Amount);
            
        var allProgressInMonth = await context.WorkOrderProgresses
            .Where(p => p.StartTime >= filterStart && p.StartTime <= filterEnd && p.EndTime != null)
            .ToListAsync();
            
        double totalLaborHoursInMonth = allProgressInMonth.Sum(p => (p.EndTime.Value - p.StartTime.Value).TotalHours);
        
        // Calculate Overhead per Hour
        decimal overheadRatePerHour = totalLaborHoursInMonth > 0 
            ? totalOverheadInMonth / (decimal)totalLaborHoursInMonth 
            : 0;

        // 2. Process Work Orders for Cost Analysis
        var workOrdersQuery = context.WorkOrders
            .Include(wo => wo.WorkOrderItems)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (startDate.HasValue)
            workOrdersQuery = workOrdersQuery.Where(wo => wo.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            workOrdersQuery = workOrdersQuery.Where(wo => wo.CreatedAt <= endDate.Value);

        var workOrders = await workOrdersQuery
            .OrderByDescending(wo => wo.CreatedAt)
            .Take(50)
            .ToListAsync();

        var results = new List<CostDetailItem>();

        foreach (var wo in workOrders)
        {
            foreach (var item in wo.WorkOrderItems)
            {
                // A. Material Cost from BOM
                var bomItems = await context.Boms
                    .Include(b => b.Child)
                    .Where(b => b.ParentId == item.ProductId)
                    .ToListAsync();
                
                decimal matCost = 0;
                foreach(var bom in bomItems) {
                    matCost += bom.QuantityPerUnit * (bom.Child?.UnitPrice ?? 10000);
                }
                matCost *= item.TargetQty;

                // B. Labor Cost & Hours from Progress
                var progress = await context.WorkOrderProgresses
                    .Include(p => p.Worker)
                        .ThenInclude(u => u.Employee)
                    .Where(p => p.WorkOrderItemId == item.ItemId)
                    .ToListAsync();
                
                decimal laborCost = 0;
                double productTotalHours = 0;
                
                foreach(var p in progress) {
                    if (p.StartTime != null && p.EndTime != null) {
                        var hours = (p.EndTime.Value - p.StartTime.Value).TotalHours;
                        productTotalHours += hours;
                        
                        // Use actual hourly rate from Employee profile
                        decimal hourlyRate = 50000; // Fallback
                        if (p.Worker?.Employee?.BasicSalary != null) {
                            // Assume 208 working hours per month (26 days * 8 hours)
                            hourlyRate = p.Worker.Employee.BasicSalary.Value / 208m;
                        }
                        laborCost += (decimal)hours * hourlyRate;
                    }
                }

                // C. Dynamic Overhead Cost
                // If overheadRatePerHour is 0 (no data), fallback to 10% of material cost
                decimal overheadCost = (overheadRatePerHour > 0)
                    ? (decimal)productTotalHours * overheadRatePerHour
                    : matCost * 0.1m;

                results.Add(new CostDetailItem {
                    Id = $"P-{item.ProductId}",
                    MaterialId = item.ProductId ?? 0,
                    Name = item.Product?.MaterialName ?? $"Sản phẩm {item.ProductId}",
                    Category = item.Product?.Category ?? "Chưa phân loại",
                    OrderRef = wo.Wocode ?? $"WO-{wo.Woid}",
                    Quantity = item.TargetQty,
                    MaterialCost = matCost,
                    LaborCost = laborCost,
                    OverheadCost = overheadCost
                });
            }
        }

        return new ProductionCostResult
        {
            Items = results,
            TotalOverhead = totalOverheadInMonth,
            TotalLaborHours = totalLaborHoursInMonth,
            OverheadRatePerHour = overheadRatePerHour
        };
    }
}
