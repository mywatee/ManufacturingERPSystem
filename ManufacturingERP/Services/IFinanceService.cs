using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingERP.Models;

namespace ManufacturingERP.Services;

public interface IFinanceService
{
    Task<List<FinancialTransaction>> GetTransactionsAsync();
    Task<List<Invoice>> GetInvoicesAsync(string type); // AR or AP
    Task<bool> AddTransactionAsync(FinancialTransaction transaction);
    Task<bool> AddInvoiceAsync(Invoice invoice);
    Task<bool> UpdateInvoiceAsync(Invoice invoice);
    Task<bool> DeleteInvoiceAsync(int invoiceId);
    Task<bool> DeleteTransactionAsync(int transactionId);
    Task<(decimal Inflow, decimal Outflow)> GetMonthlyCashFlowAsync(int month, int year);
    Task<ProductionCostResult> CalculateProductionCostsAsync(DateTime? startDate = null, DateTime? endDate = null);
}

public class ProductionCostResult
{
    public List<CostDetailItem> Items { get; set; } = new();
    public decimal TotalOverhead { get; set; }
    public double TotalLaborHours { get; set; }
    public decimal OverheadRatePerHour { get; set; }
}

public class CostDetailItem
{
    public string Id { get; set; } = "";
    public int MaterialId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string OrderRef { get; set; } = "";
    public int Quantity { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost => MaterialCost + LaborCost + OverheadCost;
    public decimal UnitPrice => Quantity > 0 ? TotalCost / Quantity : 0;
}
