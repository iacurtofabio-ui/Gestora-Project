using GestoraWebApi.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestoraWebApi.Tests.Infrastructure
{
    /// <summary>
    /// Il provider InMemory non applica gli unique index, quindi la violazione del vincolo
    /// UX_PrenotazionePostazione_Slot non e' riproducibile in un test unitario contro un DbContext.
    /// Qui si copre l'anello che conta e che si puo' isolare: il riconoscimento dell'errore
    /// Postgres 23505 dentro l'eccezione che EF Core solleva.
    /// </summary>
    public class DbExceptionTranslatorTests
    {
        private const string SlotConstraint = "UX_PrenotazionePostazione_Slot";

        private static PostgresException PostgresError(string sqlState, string? constraintName)
            => new PostgresException(
                messageText: "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: sqlState,
                constraintName: constraintName);

        [Fact]
        public void IsUniqueViolation_ConVincoloAtteso_RestituisceTrue()
        {
            var ex = new DbUpdateException("errore", PostgresError("23505", SlotConstraint));

            Assert.True(DbExceptionTranslator.IsUniqueViolation(ex, SlotConstraint));
        }

        [Fact]
        public void IsUniqueViolation_SenzaNomeVincolo_RiconosceQualunque23505()
        {
            var ex = new DbUpdateException("errore", PostgresError("23505", "UX_Altro"));

            Assert.True(DbExceptionTranslator.IsUniqueViolation(ex));
        }

        [Fact]
        public void IsUniqueViolation_VincoloDiverso_RestituisceFalse()
        {
            var ex = new DbUpdateException("errore", PostgresError("23505", "UX_Altro"));

            Assert.False(DbExceptionTranslator.IsUniqueViolation(ex, SlotConstraint));
        }

        [Fact]
        public void IsUniqueViolation_AltroCodiceErrore_RestituisceFalse()
        {
            // 23503 = foreign key violation: non e' un conflitto di slot.
            var ex = new DbUpdateException("errore", PostgresError("23503", SlotConstraint));

            Assert.False(DbExceptionTranslator.IsUniqueViolation(ex, SlotConstraint));
        }

        [Fact]
        public void IsUniqueViolation_EccezioneNonPostgres_RestituisceFalse()
        {
            var ex = new DbUpdateException("errore", new InvalidOperationException("interno"));

            Assert.False(DbExceptionTranslator.IsUniqueViolation(ex, SlotConstraint));
        }

        [Fact]
        public void IsUniqueViolation_Null_RestituisceFalse()
        {
            Assert.False(DbExceptionTranslator.IsUniqueViolation(null, SlotConstraint));
        }
    }
}
