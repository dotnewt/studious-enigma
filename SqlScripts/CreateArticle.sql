CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Articles" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "Author" text NOT NULL,
    "Content" text NOT NULL,
    "Views" integer NOT NULL,
    "UpVotes" integer NOT NULL,
    CONSTRAINT "PK_Articles" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20230111203600_InitialCreate', '7.0.2');

COMMIT;

