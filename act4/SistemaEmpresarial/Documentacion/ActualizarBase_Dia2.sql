-- Día 2 - Actividad #4
-- Ejecutar una vez si la base SistemaEmpresarialDB YA EXISTÍA desde el Día 1.
-- Si se elimina y recrea la base de datos, NO es necesario ejecutar este script.

CREATE TABLE IF NOT EXISTS "Transactions" (
    "Id" text NOT NULL,
    "AccountId" text NOT NULL,
    "ConceptId" text NOT NULL,
    "TransactionType" integer NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "TransactionDate" timestamp with time zone NOT NULL,
    "CapturedAt" timestamp with time zone NOT NULL,
    "CapturedBy" text NOT NULL,
    "ShortDescription" text NULL,
    "LongDescription" text NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Transactions_Accounts_AccountId"
        FOREIGN KEY ("AccountId") REFERENCES "Accounts" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Transactions_Concepts_ConceptId"
        FOREIGN KEY ("ConceptId") REFERENCES "Concepts" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Transactions_Users_CapturedBy"
        FOREIGN KEY ("CapturedBy") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Transactions_AccountId"
    ON "Transactions" ("AccountId");

CREATE INDEX IF NOT EXISTS "IX_Transactions_ConceptId"
    ON "Transactions" ("ConceptId");

CREATE INDEX IF NOT EXISTS "IX_Transactions_CapturedBy"
    ON "Transactions" ("CapturedBy");

CREATE INDEX IF NOT EXISTS "IX_Transactions_TransactionDate"
    ON "Transactions" ("TransactionDate");
