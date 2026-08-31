using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestoraWebApi.Migrations
{
    /// <inheritdoc />
    // Migration intenzionalmente vuota: generata per applicare la conversione EF Core
    // Stato → string (HasConversion<string>() in GestoraContext), ma la colonna era già
    // "text" fin dalla migration Initial (20251120171458). EF non ha rilevato alcuna
    // differenza di schema da applicare. Già applicata in produzione (no-op, nessun rischio
    // a lasciarla) — non rimossa dallo storico per non disallineare __EFMigrationsHistory.
    public partial class StatoAsEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
