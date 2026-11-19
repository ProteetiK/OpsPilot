
CREATE TABLE IF NOT EXISTS public."Incidents"
(
    "Id" UUID PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(5000) NOT NULL,
    "Severity" VARCHAR(20) NOT NULL DEFAULT 'Medium',
    "Status" VARCHAR(30) NOT NULL DEFAULT 'Open',
    "ServiceId" UUID NULL,
    "AssignedToUserId" UUID NULL,
    "CreatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAtUtc" TIMESTAMP WITH TIME ZONE NOT NULL,

    CONSTRAINT "FK_Incidents_Services_ServiceId"
        FOREIGN KEY ("ServiceId")
        REFERENCES public."Services" ("Id")
        ON DELETE SET NULL,

    CONSTRAINT "FK_Incidents_Users_AssignedToUserId"
        FOREIGN KEY ("AssignedToUserId")
        REFERENCES public."Users" ("Id")
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_Incidents_Status"
    ON public."Incidents" ("Status");

CREATE INDEX IF NOT EXISTS "IX_Incidents_Severity"
    ON public."Incidents" ("Severity");

CREATE INDEX IF NOT EXISTS "IX_Incidents_ServiceId"
    ON public."Incidents" ("ServiceId");

CREATE INDEX IF NOT EXISTS "IX_Incidents_AssignedToUserId"
    ON public."Incidents" ("AssignedToUserId");

INSERT INTO public."SchemaMigrations" ("Id")
VALUES ('005_AddIncidents')
ON CONFLICT ("Id") DO NOTHING;
