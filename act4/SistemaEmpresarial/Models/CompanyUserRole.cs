using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    // Une la membresía (CompanyUser) con su rol. Solo una pareja activa por membresía.
    public class CompanyUserRole
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string CompanyUserId { get; set; } = string.Empty;

        [Required]
        public string RoleId { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CompanyUser CompanyUser { get; set; } = null!;

        public Role Role { get; set; } = null!;
    }
}
