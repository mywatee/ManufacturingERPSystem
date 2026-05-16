using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ManufacturingERP.Models;

public class FinancialTransaction
{
    [Key]
    public int TransactionId { get; set; }
    
    [NotMapped]
    public string TransactionCode => $"TX{TransactionId:D4}";

    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "Chi"; // Thu or Chi
    
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = ""; // Lương, Vật tư, Bán hàng, Khác
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(255)]
    public string Description { get; set; } = "";
    
    [MaxLength(50)]
    public string? Reference { get; set; } // Link to Invoice or Payroll
    
    [MaxLength(50)]
    public string? OrderRef { get; set; }
    
    [MaxLength(50)]
    public string Method { get; set; } = "Chuyển khoản";

    public bool IsOverhead { get; set; } = false;
}

public class Invoice
{
    [Key]
    public int InvoiceId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string InvoiceCode { get; set; } = "";
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "AP"; // AP (Payable) or AR (Receivable)
    
    public int PartnerId { get; set; }
    [ForeignKey("PartnerId")]
    public virtual Partner Partner { get; set; } = null!;
    
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal VatRate { get; set; } = 10; // Default 10%
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }
    
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    
    [MaxLength(50)]
    public string? Status { get; set; } = "Chưa thanh toán"; // Đã thanh toán, Một phần, Quá hạn
    
    [MaxLength(100)]
    public string? Reference { get; set; } // External document number or link
    
    public string? Note { get; set; }
    
    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}

public partial class InvoiceItem : ObservableValidator
{
    [Key]
    public int InvoiceItemId { get; set; }
    
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public virtual Invoice Invoice { get; set; } = null!;
    
    [ObservableProperty]
    [Required]
    [MaxLength(255)]
    private string _productName = "";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private decimal _quantity;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private decimal _unitPrice;
    
    public decimal TotalPrice => Quantity * UnitPrice;
}
