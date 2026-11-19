CREATE TABLE IF NOT EXISTS public."AuditLogs"
(
    "Id" UUID PRIMARY KEY,
    "ActorUserId" UUID NOT NULL,
    "TargetUserId" UUID NOT NULL,
    "Action" VARCHAR(100) NOT NULL,
    "OldValue" VARCHAR(100),
    "NewValue" VARCHAR(100),
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_ActorUserId"
    ON public."AuditLogs" ("ActorUserId");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TargetUserId"
    ON public."AuditLogs" ("TargetUserId");

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAtUtc"
    ON public."AuditLogs" ("CreatedAtUtc");

INSERT INTO public."SchemaMigrations" ("Id")
VALUES ('004_AddAuditLogs')
ON CONFLICT ("Id") DO NOTHING;