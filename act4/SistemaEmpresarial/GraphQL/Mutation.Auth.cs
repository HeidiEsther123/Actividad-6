using HotChocolate;
using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Acceso;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.Models;

namespace SistemaEmpresarial.GraphQL
{
    // companyId es opcional: solo hace falta si la persona pertenece a más de una empresa.
    public record LoginInput(
        string Email,
        string Password,
        [property: ID] string? CompanyId = null);

    public class AuthPayload
    {
        public string Token { get; init; } = string.Empty;
        public string TokenType { get; init; } = "bearer";
        public User User { get; init; } = null!;
    }

    public partial class Mutation
    {
        // Verifica el hash. Si es correcto, firma el token con la membresía.
        // El companyId lo toma el servidor de la membresía, no del cliente.
        public async Task<AuthPayload> LoginAsync(LoginInput input, AppDbContext context)
        {
            var email = input.Email.Trim().ToLowerInvariant();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Mismo mensaje para correo inexistente y contraseña incorrecta
            if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
                throw new GraphQLException("Credenciales incorrectas");

            var membresias = await context.CompanyUsers
                .Include(cu => cu.Company)
                .Where(cu => cu.UserId == user.Id && cu.IsActive && cu.Company != null && cu.Company.IsActive)
                .ToListAsync();

            CompanyUser membresia;

            if (input.CompanyId != null)
            {
                membresia = membresias.FirstOrDefault(cu => cu.CompanyId == input.CompanyId)
                    ?? throw new GraphQLException("La persona no tiene una membresía activa en esa empresa");
            }
            else if (membresias.Count == 1)
            {
                membresia = membresias[0];
            }
            else if (membresias.Count == 0)
            {
                throw new GraphQLException("La persona no tiene membresías activas");
            }
            else
            {
                throw new GraphQLException("Pertenece a varias empresas; indique companyId");
            }

            return new AuthPayload
            {
                Token = TokenService.CrearToken(membresia.Id, membresia.CompanyId),
                TokenType = "bearer",
                User = user
            };
        }
    }
}
