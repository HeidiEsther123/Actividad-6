# Actividad 6 — Autenticación JWT y control de acceso por roles (GraphQL)

**Alumna:** Heidi Esther Peña Betanzos
**Materia:** Unidad 2 — Actividad 6

API GraphQL en **ASP.NET Core (.NET 10)** con **HotChocolate**, **Entity Framework Core** y **PostgreSQL**
que implementa autenticación con **JWT**, roles, permisos y autorización por recurso.

## Estructura del repositorio

```
act4/
├── SistemaEmpresarial/          # Proyecto principal (API GraphQL)
│   ├── Acceso/                  # JWT, roles, permisos y control de acceso
│   ├── Data/                    # DbContext y configuración de EF Core
│   ├── GraphQL/                 # Query y Mutation
│   ├── Models/                  # Entidades del dominio
│   ├── Documentacion/           # Guías y scripts de pruebas
│   └── appsettings.json         # Cadena de conexión
├── SistemaEmpresarial.Tests/    # Pruebas
├── variables/                   # Variables de ejemplo para GraphiQL
├── comprobaciones.sql           # Consultas de verificación en la base de datos
├── configuracion_inicial.json   # Matriz inicial de roles y permisos
├── operaciones.graphql          # Operaciones base
├── operaciones_actividad6.graphql # Operaciones de la actividad 6
└── SistemaEmpresarial.slnx      # Solución
```

## Requisitos

- .NET SDK 10
- PostgreSQL

## Configuración y ejecución

1. Ajustar la cadena de conexión en `act4/SistemaEmpresarial/appsettings.json`.
2. Restaurar dependencias y ejecutar:

```bash
cd act4
dotnet restore
dotnet run --project SistemaEmpresarial
```

La base de datos se crea automáticamente al iniciar (`EnsureCreated`) y se cargan los roles,
permisos y la matriz inicial mediante `AccessSeeder`.

3. Abrir GraphiQL en `https://localhost:<puerto>/graphql`.

## Uso

El flujo de la actividad se describe en `act4/operaciones_actividad6.graphql`:

1. Ejecutar el `login` (sin encabezado `Authorization`).
2. Copiar el token y enviarlo como `Authorization: Bearer <token>`.
3. Ejecutar las operaciones protegidas (cuentas, movimientos, roles, etc.).

## Notas

- `appsettings.json` contiene credenciales de un PostgreSQL **local**; se recomienda usar
  *user secrets* o variables de entorno en entornos reales.
