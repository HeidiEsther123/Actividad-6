# Pruebas — Actividad 3: roles y permisos

Todas las operaciones reciben `actorCompanyUserId`: el **id de la membresía** (tabla `CompanyUsers`) con la que se prueba.
Ya no hace falta el header `X-User-Id` para estas operaciones.

## 0. Arranque

Al arrancar, `AccessSeeder` crea las tablas nuevas si faltan, inserta 4 roles, 10 permisos y la matriz (solo si están vacías) y
le da el rol ADMINISTRADOR a los administradores que ya existían. Reiniciar no duplica nada.

## 1. Preparar empresa y administrador

```graphql
mutation { createCompany(input: { name: "Empresa A" }) { id } }
```

```graphql
mutation {
  createAdmin(input: {
    companyId: "COMPANY_ID", name: "Admin A",
    email: "admin@a.com", password: "123456"
  }) { id name }
}
```

El primer `actorCompanyUserId` (el del administrador) se obtiene una sola vez en PostgreSQL:

```sql
SELECT cu."Id" AS company_user_id, u."Email", cu."IsAdmin"
FROM "CompanyUsers" cu JOIN "Users" u ON u."Id" = cu."UserId";
```

Con ese id ya se pueden usar las consultas. Los demás ids salen de `companyUsers`.

## 2. Roles y permisos (capturas)

```graphql
query { roles(actorCompanyUserId: "ADMIN_CU_ID") { id code name isActive } }
```
Debe mostrar ADMINISTRADOR, CONTADOR, CAPTURISTA y CONSULTA.

```graphql
query { permissions(actorCompanyUserId: "ADMIN_CU_ID") { code } }
```
Deben ser los diez códigos.

```graphql
query {
  rolePermissions(roleId: "ROLE_CAPTURISTA_ID", actorCompanyUserId: "ADMIN_CU_ID") {
    permission { code }
  }
}
```
Incluye `transactions.write` y NO incluye `accounts.write`. Repite con el rol CONSULTA: ningún código termina en `.write`.

## 3. Crear usuarios y asignarles rol

```graphql
mutation {
  createUser(actorCompanyUserId: "ADMIN_CU_ID", input: {
    companyId: "COMPANY_ID", name: "Ana", email: "ana@a.com", password: "123456"
  }) { id }
}
```

```graphql
query {
  companyUsers(companyId: "COMPANY_ID", actorCompanyUserId: "ADMIN_CU_ID") {
    id isAdmin user { name email }
  }
}
```

```graphql
mutation {
  assignRole(actorCompanyUserId: "ADMIN_CU_ID", input: {
    companyUserId: "ANA_CU_ID", roleId: "ROLE_CAPTURISTA_ID"
  }) { id isActive role { code } }
}
```

Repite con otros dos usuarios: uno CONSULTA y otro CONTADOR.

## 4. Movimiento aceptado y rechazado

Necesitas una cuenta, un concepto y su relación (hazlo con el administrador o el contador):
`createAccount`, `createConcept`, `assignConceptToAccount`, todos con `actorCompanyUserId`.

```graphql
mutation {
  createTransaction(actorCompanyUserId: "CAPTURISTA_CU_ID", input: {
    accountId: "ACCOUNT_ID", conceptId: "CONCEPT_ID",
    transactionType: INCOME, amount: 100
  }) { id amount }
}
```
Con CAPTURISTA se guarda. Con el mismo mutation y `CONSULTA_CU_ID` responde `errors` ("El rol no tiene el permiso transactions.write")
y en la tabla `Transactions` no aparece la fila.

## 5. createAccount: contador sí, capturista no

```graphql
mutation {
  createAccount(actorCompanyUserId: "CONTADOR_CU_ID", input: {
    companyId: "COMPANY_ID", accountType: CASH, name: "Caja"
  }) { id name }
}
```
Con `CAPTURISTA_CU_ID` se rechaza.

## 6. assignRole cambia el puesto

Cambia a Ana de CAPTURISTA a CONTADOR con `assignRole` (el admin como actor). Después Ana ya puede ejecutar `createAccount`.
Revisa que solo quede un rol activo:

```graphql
query {
  companyUserRoles(companyId: "COMPANY_ID", actorCompanyUserId: "ADMIN_CU_ID") {
    companyUser { user { name } } role { code } isActive
  }
}
```

## 7. Un segundo administrador sigue fallando

```graphql
mutation {
  createAdmin(input: {
    companyId: "COMPANY_ID", name: "Otro", email: "otro@a.com", password: "123456"
  }) { id }
}
```
Debe responder "La empresa ya tiene un administrador principal".

## 8. Verificar en PostgreSQL y tras reiniciar

```sql
SELECT "Code", "Name" FROM "Roles";
SELECT "Code" FROM "Permissions";
SELECT r."Code" AS rol, p."Code" AS permiso
FROM "RolePermissions" rp
JOIN "Roles" r ON r."Id" = rp."RoleId"
JOIN "Permissions" p ON p."Id" = rp."PermissionId"
WHERE rp."IsActive" ORDER BY r."Code", p."Code";
```
Detén y vuelve a arrancar el servidor: `SELECT count(*) FROM "Roles";` debe seguir en 4.
