using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaEmpresarial.Models
{
    // Catálogo compartido de roles (puestos). Se desactivan, no se borran.
    public class Role
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
