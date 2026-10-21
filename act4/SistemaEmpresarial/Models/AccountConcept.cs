using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    public class AccountConcept
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string AccountId { get; set; } = string.Empty;

        [Required]
        public string ConceptId { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Account? Account { get; set; }

        public Concept? Concept { get; set; }
    }
}
