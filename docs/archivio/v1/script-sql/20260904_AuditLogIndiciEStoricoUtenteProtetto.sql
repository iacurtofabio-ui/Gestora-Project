-- NB: file salvato SENZA BOM di proposito. Lo script generato da
-- 'dotnet ef migrations script' ha il BOM UTF-8 in testa e psql lo interpreta come parte
-- della prima istruzione: START TRANSACTION fallisce e l'intero script gira in autocommit,
-- cioe' senza rete di sicurezza in caso di errore. Se si rigenera lo script, ripulire il BOM.
--
-- Fase 7 - REV-037 (indici e MaxLength sull'audit trail) e REV-038 (la chiave esterna delle
-- prenotazioni verso gli utenti passa da CASCADE a RESTRICT: eliminare un utente non cancella
-- piu' il suo storico).
--
-- PRIMA DI ESEGUIRLO, controllare che nessun dato superi i nuovi limiti di lunghezza:
--   SELECT max(length("UserId")), max(length("Action")), max(length("IPAddress")) FROM "LogActivities";
-- Attesi ben sotto 450 / 500 / 45. Se un massimo fosse oltre soglia, NON troncare i dati:
-- alzare il limite nel modello e rigenerare la migration.
--
-- Non e' una migration breaking: la versione precedente dell'app continua a funzionare con
-- questo schema (gli indici e i limiti non la riguardano; l'unico comportamento diverso e' che
-- l'eliminazione di un utente con prenotazioni verrebbe rifiutata dal database invece che
-- dall'applicazione). Non serve quindi una finestra di manutenzione stretta.

START TRANSACTION;
ALTER TABLE "Prenotazioni" DROP CONSTRAINT "FK_Prenotazioni_Utenti_UserId";

ALTER TABLE "LogActivities" ALTER COLUMN "UserId" TYPE character varying(450);

ALTER TABLE "LogActivities" ALTER COLUMN "IPAddress" TYPE character varying(45);

ALTER TABLE "LogActivities" ALTER COLUMN "Action" TYPE character varying(500);

CREATE INDEX "IX_LogActivities_Timestamp" ON "LogActivities" ("Timestamp");

CREATE INDEX "IX_LogActivities_UserId_Timestamp" ON "LogActivities" ("UserId", "Timestamp");

ALTER TABLE "Prenotazioni" ADD CONSTRAINT "FK_Prenotazioni_Utenti_UserId" FOREIGN KEY ("UserId") REFERENCES "Utenti" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260904141854_AuditLogIndiciEStoricoUtenteProtetto', '9.0.9');

COMMIT;

