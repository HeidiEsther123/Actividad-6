# Pruebas del Día 2 — Motor de movimientos

## 1. Obtener usuario autenticado para las pruebas

Ejecuta el login del Día 1 y copia `userId`:

```graphql
mutation {
  login(input: {
    email: "CORREO_DEL_USUARIO"
    password: "CONTRASEÑA"
  }) {
    success
    userId
    name
    email
  }
}
```

## 2. Obtener cuentas y conceptos

```graphql
query {
  accounts(companyId: "COMPANY_ID", activeOnly: true) {
    id
    name
    accountType
  }

  concepts(companyId: "COMPANY_ID", activeOnly: true) {
    id
    name
    conceptType
  }
}
```

Usa una cuenta y un concepto que ya estén relacionados mediante `AccountConcept`.

## 3. Crear un ingreso

Header requerido para las pruebas de autenticación:

```text
X-User-Id: USER_ID
```

Mutation:

```graphql
mutation {
  createTransaction(input: {
    accountId: "ACCOUNT_ID"
    conceptId: "INCOME_CONCEPT_ID"
    transactionType: INCOME
    amount: 1500.00
    shortDescription: "Venta"
    longDescription: "Ingreso de prueba"
  }) {
    id
    accountId
    conceptId
    transactionType
    amount
    transactionDate
    capturedAt
    capturedBy
    isActive
    account { id name }
    concept { id name conceptType }
    capturedByUser { id name email }
  }
}
```

## 4. Crear un gasto

```graphql
mutation {
  createTransaction(input: {
    accountId: "ACCOUNT_ID"
    conceptId: "EXPENSE_CONCEPT_ID"
    transactionType: EXPENSE
    amount: 350.00
    shortDescription: "Compra"
    longDescription: "Gasto de prueba"
  }) {
    id
    transactionType
    amount
    capturedBy
    account { id name }
    concept { id name conceptType }
  }
}
```

## 5. Consultar movimientos

```graphql
query {
  transactions(limit: 20, offset: 0) {
    id
    accountId
    conceptId
    transactionType
    amount
    transactionDate
    capturedAt
    capturedBy
    shortDescription
    isActive
    account { id name }
    concept { id name conceptType }
    capturedByUser { id name }
  }
}
```

Solo se devuelven movimientos activos y pertenecientes a empresas a las que pertenece el usuario autenticado.

## 6. Filtro por cuenta

```graphql
query {
  transactions(accountId: "ACCOUNT_ID", limit: 20) {
    id
    transactionType
    amount
    transactionDate
  }
}
```

## 7. Filtro por tipo

```graphql
query {
  transactions(transactionType: INCOME, limit: 20) {
    id
    transactionType
    amount
  }
}
```

## 8. Eliminar lógicamente un movimiento

```graphql
mutation {
  deleteTransaction(id: "TRANSACTION_ID") {
    id
    isActive
    updatedAt
  }
}
```

## 9. Casos de validación que deben producir error

### Monto cero

```graphql
mutation {
  createTransaction(input: {
    accountId: "ACCOUNT_ID"
    conceptId: "INCOME_CONCEPT_ID"
    transactionType: INCOME
    amount: 0
  }) {
    id
  }
}
```

Debe responder con error indicando que el monto debe ser mayor que 0.

### Monto negativo

Usa `amount: -100`.

### Concepto no permitido para la cuenta

Usa un concepto que exista pero que NO esté asignado activamente a la cuenta.

### Empresa diferente

Usa una cuenta de una empresa y un concepto de otra.

### Tipo incompatible

Usa un concepto `EXPENSE` con `transactionType: INCOME`, o viceversa.

### Usuario sin pertenencia a la empresa

Usa `X-User-Id` de un usuario que no tenga una relación `CompanyUser` activa con la empresa de la cuenta.

### Sin autenticación

No envíes `X-User-Id`. Debe responder que el usuario debe estar autenticado.

## 10. Requisito de 20 movimientos

Crea al menos 20 registros combinando `INCOME` y `EXPENSE`, utilizando únicamente conceptos permitidos para sus respectivas cuentas.

Para la evidencia, conserva:
- una captura de un ingreso creado correctamente;
- una captura de un gasto creado correctamente;
- una consulta mostrando los movimientos;
- una captura de una validación fallida;
- una captura de `deleteTransaction` mostrando `isActive: false`;
- evidencia de los 20 movimientos.
