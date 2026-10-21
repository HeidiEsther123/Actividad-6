using System;
using System.ComponentModel.DataAnnotations;
using HotChocolate;

namespace SistemaEmpresarial.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [GraphQLIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public bool EmailVerified { get; set; } = false;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}