using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestoraWebApi.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Fase 7 — REV-037 e REV-038.
    /// <para>
    /// <b>REV-037</b>: indici e limiti di lunghezza su <c>LogActivities</c>, la tabella
    /// dell'audit trail: cresce a ogni operazione, non viene mai ripulita e finora si leggeva
    /// solo con scansioni complete.
    /// </para>
    /// <para>
    /// <b>REV-038</b>: la chiave esterna Prenotazioni → Utenti passa da <c>Cascade</c> a
    /// <c>Restrict</c>. Eliminare un utente non cancella piu' il suo storico di prenotazioni.
    /// </para>
    /// <para>
    /// ⚠️ <b>Prima di applicarla in produzione.</b> Gli <c>ALTER COLUMN</c> introducono un tetto
    /// di lunghezza su colonne oggi senza limiti: se esistesse anche una sola riga piu' lunga,
    /// l'istruzione fallirebbe. Va verificato prima, con:
    /// <code>
    /// SELECT max(length("UserId")), max(length("Action")), max(length("IPAddress")) FROM "LogActivities";
    /// </code>
    /// I valori attesi sono ben sotto le soglie (450 / 500 / 45): i messaggi sono generati dal
    /// codice, l'UserId e' un GUID e l'IP arriva al massimo a 45 caratteri in IPv6. Se un
    /// massimo risultasse oltre soglia, alzare il limite nel modello invece di troncare i dati.
    /// </para>
    /// <para>
    /// Il cambio di chiave esterna non tocca i dati e non richiede finestre di fermo: cambia
    /// solo cosa il database accettera' da qui in avanti.
    /// </para>
    /// </summary>
    public partial class AuditLogIndiciEStoricoUtenteProtetto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prenotazioni_Utenti_UserId",
                table: "Prenotazioni");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LogActivities",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "LogActivities",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "LogActivities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_LogActivities_Timestamp",
                table: "LogActivities",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LogActivities_UserId_Timestamp",
                table: "LogActivities",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_Prenotazioni_Utenti_UserId",
                table: "Prenotazioni",
                column: "UserId",
                principalTable: "Utenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prenotazioni_Utenti_UserId",
                table: "Prenotazioni");

            migrationBuilder.DropIndex(
                name: "IX_LogActivities_Timestamp",
                table: "LogActivities");

            migrationBuilder.DropIndex(
                name: "IX_LogActivities_UserId_Timestamp",
                table: "LogActivities");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LogActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "LogActivities",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "LogActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_Prenotazioni_Utenti_UserId",
                table: "Prenotazioni",
                column: "UserId",
                principalTable: "Utenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
