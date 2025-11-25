-- SQL migration: AddKeycloakId to Profiles
-- Run this against the UserService Postgres database. This is a safe, idempotent migration (uses IF NOT EXISTS).

ALTER TABLE IF EXISTS "Profiles"
    ADD COLUMN IF NOT EXISTS "KeycloakId" text;

-- Optionally create an index on KeycloakId for faster lookups
CREATE INDEX IF NOT EXISTS IX_Profiles_KeycloakId ON "Profiles" ("KeycloakId");

