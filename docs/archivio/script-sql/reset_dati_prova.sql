-- Reset dei dati di prova su Neon (produzione) — Fase 11, BACKLOG OPS-001.
--
-- Il database Neon è nato il 16/09/2026 e contiene solo la prova end-to-end del 18/09/2026:
-- una zona, un tavolo, una fascia, una prenotazione, un utente Staff e uno Cliente di prova,
-- oltre al primo Admin creato dalla schermata di primo avvio.
--
-- Cosa fa: svuota tutti i dati di dominio (RESTART IDENTITY CASCADE azzera anche i contatori
-- degli id) e cancella ogni utente che NON ha il ruolo Admin. NON tocca le tabelle QRTZ_* (Quartz)
-- né __EFMigrationsHistory: quelle non sono dati di dominio, cancellarle rompe l'applicazione.
--
-- ⚠️ Prima di eseguirlo: fare un backup (RUNBOOK.md §6, `pg_dump`). Non è reversibile.
--
-- Uso:
--   psql "<connection string di Neon>" -f Scripts\reset_dati_prova.sql

BEGIN;

-- 1. Dati di dominio: l'ordine rispetta le foreign key (le righe di collegamento prima delle
--    prenotazioni, i tavoli prima delle zone, ecc.). CASCADE copre comunque i riferimenti residui.
TRUNCATE TABLE
    "PrenotazioniPostazioni",
    "Prenotazioni",
    "Postazioni",
    "FasceOrarie",
    "Zone",
    "LogActivities"
RESTART IDENTITY CASCADE;

-- 2. Utenti: cancella solo chi NON ha il ruolo Admin. La tabella di collegamento utente-ruolo
--    di ASP.NET Identity non è stata rinominata (a differenza di Utenti/Ruoli), resta
--    "AspNetUserRoles" — verificare in Context/GestoraContext.cs se questo script smette di
--    funzionare dopo una futura migrazione.
DELETE FROM "AspNetUserRoles"
WHERE "UserId" IN (
    SELECT u."Id" FROM "Utenti" u
    WHERE u."Id" NOT IN (
        SELECT ur."UserId" FROM "AspNetUserRoles" ur
        JOIN "Ruoli" r ON r."Id" = ur."RoleId"
        WHERE r."Name" = 'Admin'
    )
);

DELETE FROM "Utenti" u
WHERE u."Id" NOT IN (
    SELECT ur."UserId" FROM "AspNetUserRoles" ur
    JOIN "Ruoli" r ON r."Id" = ur."RoleId"
    WHERE r."Name" = 'Admin'
);

COMMIT;

-- Verifica dopo l'esecuzione: SELECT count(*) su ognuna delle tabelle sopra deve dare 0
-- (tranne "Utenti", dove deve restare solo l'Admin).
