using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    public class Company
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}