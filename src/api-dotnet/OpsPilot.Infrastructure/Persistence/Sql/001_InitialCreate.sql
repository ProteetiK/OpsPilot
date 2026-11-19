
-- OpsPilot: Initial database schema
-- PostgreSQL 16

CREATE TABLE IF NOT EXISTS "Services"
(
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(150) NOT NULL,
    "Description" VARCHAR(1000) NOT NULL DEFAULT '',
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "IX_Services_Name"
ON "Services" ("Name");

-- Track manually applied SQL migrations
CREATE TABLE IF NOT EXISTS "SchemaMigrations"
(
    "Id" VARCHAR(100) PRIMARY KEY,
    "AppliedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO "SchemaMigrations" ("Id")
VALUES ('001_InitialCreate')
ON CONFLICT ("Id") DO NOTHING;
