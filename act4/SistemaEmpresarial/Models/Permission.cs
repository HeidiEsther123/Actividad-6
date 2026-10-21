using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    // Acción sobre una parte del sistema. El código se lee recurso.accion
    public class Permission
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
