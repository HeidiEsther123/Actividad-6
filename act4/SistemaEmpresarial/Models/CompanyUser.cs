using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    public class CompanyUser
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CompanyId { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public bool IsAdmin { get; set; } = false;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public Company? Company { get; set; }

        public User? User { get; set; }
    }
}