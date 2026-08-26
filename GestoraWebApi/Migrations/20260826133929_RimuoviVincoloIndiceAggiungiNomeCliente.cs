using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestoraWebApi.Migrations
{
    /// <inheritdoc />
    public partial class RimuoviVincoloIndiceAggiungiNomeCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Prenotazione_User_DataPrenotazione",
                table: "Prenotazioni");

            migrationBuilder.AddColumn<string>(
                name: "NomeCliente",
                table: "Prenotazioni",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prenotazioni_UserId",
                table: "Prenotazioni",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Prenotazioni_UserId",
                table: "Prenotazioni");

            migrationBuilder.DropColumn(
                name: "NomeCliente",
                table: "Prenotazioni");

            migrationBuilder.CreateIndex(
                name: "UX_Prenotazione_User_DataPrenotazione",
                table: "Prenotazioni",
                columns: new[] { "UserId", "DataPrenotazione" },
                unique: true,
                filter: "\"Stato\" <> 'Annullata'");
        }
    }
}
