using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEmpresarial.Models
{
    public enum TransactionType
    {
        INCOME,
        EXPENSE
    }

    public class Transaction
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string AccountId { get; set; } = string.Empty;

        [Required]
        public string ConceptId { get; set; } = string.Empty;

        [Required]
        public TransactionType TransactionType { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string CapturedBy { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Account? Account { get; set; }
        public Concept? Concept { get; set; }
        public User? CapturedByUser { get; set; }
    }
}
