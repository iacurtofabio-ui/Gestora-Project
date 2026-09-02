START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902081401_AggiungiSlotPrenotazionePostazione') THEN

                    ALTER TABLE "PrenotazioniPostazioni" ADD COLUMN "DataPrenotazione" date NULL;
                    ALTER TABLE "PrenotazioniPostazioni" ADD COLUMN "FasciaOrariaId" bigint NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902081401_AggiungiSlotPrenotazionePostazione') THEN

                    UPDATE "PrenotazioniPostazioni" pp
                    SET "DataPrenotazione" = p."DataPrenotazione",
                        "FasciaOrariaId"   = p."FasciaOrariaId"
                    FROM "Prenotazioni" p
                    WHERE p."Id" = pp."PrenotazioneId";
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902081401_AggiungiSlotPrenotazionePostazione') THEN

                    DELETE FROM "PrenotazioniPostazioni" pp
                    USING "Prenotazioni" p
                    WHERE p."Id" = pp."PrenotazioneId"
                      AND p."Stato" = 'Annullata';
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902081401_AggiungiSlotPrenotazionePostazione') THEN

                    ALTER TABLE "PrenotazioniPostazioni" ALTER COLUMN "DataPrenotazione" SET NOT NULL;
                    ALTER TABLE "PrenotazioniPostazioni" ALTER COLUMN "FasciaOrariaId" SET NOT NULL;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902081401_AggiungiSlotPrenotazionePostazione') THEN
    CREATE UNIQUE INDEX "UX_PrenotazionePostazione_Slot" ON "PrenotazioniPostazioni" ("PostazioneId", "DataPrenotazione", "FasciaOrariaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260902081401_AggiungiSlotPrenotazionePostazione') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260902081401_AggiungiSlotPrenotazionePostazione', '9.0.9');
    END IF;
END $EF$;
COMMIT;

