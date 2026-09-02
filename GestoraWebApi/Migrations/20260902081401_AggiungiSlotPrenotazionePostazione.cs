using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestoraWebApi.Migrations
{
    /// <summary>
    /// REV-003 — Denormalizza lo slot (data + fascia oraria) sulle righe di
    /// PrenotazioniPostazioni e ci appoggia sopra l'unique index UX_PrenotazionePostazione_Slot,
    /// che rende fisicamente impossibile assegnare lo stesso tavolo a due prenotazioni nello
    /// stesso slot.
    ///
    /// MIGRATION BREAKING, SCRITTA A MANO. Lo scaffolding automatico di EF non e' utilizzabile
    /// qui: aggiungerebbe le due colonne NOT NULL con default (0001-01-01 e 0) su tutte le righe
    /// esistenti e poi fallirebbe la creazione dell'indice al primo tavolo con piu' di una
    /// prenotazione storica. L'ordine sotto (nullable -> backfill -> pulizia -> NOT NULL ->
    /// indice) e' vincolante.
    ///
    /// Prima di applicarla in produzione: pg_dump di backup e query di pre-check dei duplicati
    /// (procedura in testa a ROADMAP_REVISIONE.md).
    /// </summary>
    public partial class AggiungiSlotPrenotazionePostazione : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Colonne nuove, per ora nullable: le righe gia' in tabella non hanno il dato.
            migrationBuilder.Sql(@"
                ALTER TABLE ""PrenotazioniPostazioni"" ADD COLUMN ""DataPrenotazione"" date NULL;
                ALTER TABLE ""PrenotazioniPostazioni"" ADD COLUMN ""FasciaOrariaId"" bigint NULL;
            ");

            // 2. Backfill dallo slot della prenotazione di appartenenza.
            migrationBuilder.Sql(@"
                UPDATE ""PrenotazioniPostazioni"" pp
                SET ""DataPrenotazione"" = p.""DataPrenotazione"",
                    ""FasciaOrariaId""   = p.""FasciaOrariaId""
                FROM ""Prenotazioni"" p
                WHERE p.""Id"" = pp.""PrenotazioneId"";
            ");

            // 3. Una prenotazione annullata libera il tavolo: le sue righe join non devono piu'
            //    esistere (nuovo comportamento di AnnullaPrenotazioneAsync). Allinea lo storico
            //    alla regola nuova ed evita che una vecchia annullata blocchi l'indice unique.
            //    NB: Prenotazione.Stato e' mappato come STRINGA su DB (HasConversion<string>()).
            migrationBuilder.Sql(@"
                DELETE FROM ""PrenotazioniPostazioni"" pp
                USING ""Prenotazioni"" p
                WHERE p.""Id"" = pp.""PrenotazioneId""
                  AND p.""Stato"" = 'Annullata';
            ");

            // 4. Ora il dato c'e' su tutte le righe: il vincolo NOT NULL puo' essere applicato.
            migrationBuilder.Sql(@"
                ALTER TABLE ""PrenotazioniPostazioni"" ALTER COLUMN ""DataPrenotazione"" SET NOT NULL;
                ALTER TABLE ""PrenotazioniPostazioni"" ALTER COLUMN ""FasciaOrariaId"" SET NOT NULL;
            ");

            // 5. Il vincolo vero. Indice PIENO, nessun WHERE (decisione A2 del 01/09/2026).
            migrationBuilder.CreateIndex(
                name: "UX_PrenotazionePostazione_Slot",
                table: "PrenotazioniPostazioni",
                columns: new[] { "PostazioneId", "DataPrenotazione", "FasciaOrariaId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Attenzione: il Down ripristina lo schema, non i dati. Le righe cancellate al
            // passo 3 non tornano indietro — la rete di sicurezza e' il pg_dump.
            migrationBuilder.DropIndex(
                name: "UX_PrenotazionePostazione_Slot",
                table: "PrenotazioniPostazioni");

            migrationBuilder.DropColumn(
                name: "DataPrenotazione",
                table: "PrenotazioniPostazioni");

            migrationBuilder.DropColumn(
                name: "FasciaOrariaId",
                table: "PrenotazioniPostazioni");
        }
    }
}
