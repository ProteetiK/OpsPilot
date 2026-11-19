-- OPS-101: User accounts for authentication

CREATE TABLE IF NOT EXISTS "Users"
(
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(256) NOT NULL,
    "NormalizedEmail" VARCHAR(256) NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedEmail"
ON "Users" ("NormalizedEmail");

INSERT INTO "SchemaMigrations" ("Id")
VALUES ('002_AddUsers')
ON CONFLICT ("Id") DO NOTHING;