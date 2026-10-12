using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestoraWebApi.Migrations
{
    /// <inheritdoc />
    public partial class UtenteDataCreazioneEUltimoAccesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataCreazione",
                table: "Utenti",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAccesso",
                table: "Utenti",
                type: "timestamp with time zone",
                nullable: true);

            // V2-015: per le utenze gia' presenti l'ultimo accesso si ricava dal registro
            // attivita' (riga "Login" a ogni accesso riuscito). Senza, chi e' gia' entrato
            // risulterebbe "mai usato". DataCreazione resta l'istante della migration: e' la
            // scelta prudente, i 180 giorni partono da oggi per tutti.
            migrationBuilder.Sql(@"
                UPDATE ""Utenti"" u
                SET ""UltimoAccesso"" = l.""UltimoLogin""
                FROM (
                    SELECT ""UserId"", MAX(""Timestamp"") AS ""UltimoLogin""
                    FROM ""LogActivities""
                    WHERE ""Action"" = 'Login'
                    GROUP BY ""UserId""
                ) l
                WHERE l.""UserId"" = u.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataCreazione",
                table: "Utenti");

            migrationBuilder.DropColumn(
                name: "UltimoAccesso",
                table: "Utenti");
        }
    }
}
