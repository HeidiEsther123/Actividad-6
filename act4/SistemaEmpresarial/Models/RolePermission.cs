using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    // Tabla puente Role N ── N Permission
    public class RolePermission
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string RoleId { get; set; } = string.Empty;

        [Required]
        public string PermissionId { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Role Role { get; set; } = null!;

        public Permission Permission { get; set; } = null!;
    }
}
