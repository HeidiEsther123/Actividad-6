using Microsoft.EntityFrameworkCore;
using SistemaEmpresarial.Data;
using SistemaEmpresarial.GraphQL;
using SistemaEmpresarial.Acceso;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configurar GraphQL (HotChocolate)
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .ModifyRequestOptions(options =>
        options.IncludeExceptionDetails = true
    );

var app = builder.Build();

// Crear la base de datos automáticamente si no existe (ideal para desarrollo rápido)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    // Roles, permisos y matriz inicial (no duplica si ya existen)
    AccessSeeder.Seed(context);
}

app.UseHttpsRedirection();

// 3. Habilitar el endpoint de GraphQL
app.MapGraphQL();

app.Run();
