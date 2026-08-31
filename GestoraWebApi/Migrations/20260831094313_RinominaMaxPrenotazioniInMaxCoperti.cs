using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestoraWebApi.Migrations
{
    /// <inheritdoc />
    public partial class RinominaMaxPrenotazioniInMaxCoperti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxPrenotazioni",
                table: "FasceOrarie",
                newName: "MaxCoperti");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxCoperti",
                table: "FasceOrarie",
                newName: "MaxPrenotazioni");
        }
    }
}
