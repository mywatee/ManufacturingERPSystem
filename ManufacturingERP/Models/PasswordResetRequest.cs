using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingERP.Models
{
    public class PasswordResetRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int UserId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime? ProcessedDate { get; set; }

        public int? ProcessedByAdminId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ProcessedByAdminId")]
        public virtual User? Admin { get; set; }
    }
}
