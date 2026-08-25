using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestoraWebApi.Migrations
{
    /// <inheritdoc />
    public partial class FilterIndicePrenotazioneEscludeAnnullata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Prenotazione_User_DataPrenotazione",
                table: "Prenotazioni");

            migrationBuilder.CreateIndex(
                name: "UX_Prenotazione_User_DataPrenotazione",
                table: "Prenotazioni",
                columns: new[] { "UserId", "DataPrenotazione" },
                unique: true,
                filter: "\"Stato\" <> 'Annullata'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Prenotazione_User_DataPrenotazione",
                table: "Prenotazioni");

            migrationBuilder.CreateIndex(
                name: "UX_Prenotazione_User_DataPrenotazione",
                table: "Prenotazioni",
                columns: new[] { "UserId", "DataPrenotazione" },
                unique: true);
        }
    }
}
