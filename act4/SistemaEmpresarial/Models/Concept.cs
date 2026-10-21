using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    public enum ConceptType
    {
        INCOME,
        EXPENSE
    }

    public class Concept
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CompanyId { get; set; } = string.Empty;

        [Required]
        public ConceptType ConceptType { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Company? Company { get; set; }
    }
}
