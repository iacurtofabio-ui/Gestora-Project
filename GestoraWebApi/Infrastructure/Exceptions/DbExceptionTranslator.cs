using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestoraWebApi.Infrastructure.Exceptions
{
    /// <summary>
    /// Riconosce gli errori del driver Postgres che hanno un significato di dominio.
    /// Isolato in una classe statica senza dipendenze per poter essere testato senza database:
    /// il provider InMemory non applica gli unique index, quindi la violazione va simulata.
    /// </summary>
    public static class DbExceptionTranslator
    {
        /// <summary>Codice SQLSTATE Postgres per la violazione di un vincolo di unicita'.</summary>
        private const string UniqueViolation = PostgresErrorCodes.UniqueViolation; // "23505"

        /// <summary>
        /// True se l'eccezione (o una qualsiasi delle sue inner) e' una violazione di unicita'
        /// Postgres. Se <paramref name="constraintName"/> e' valorizzato, la violazione deve
        /// riguardare proprio quel vincolo: cosi' un 23505 di un altro indice non viene scambiato
        /// per un conflitto di slot.
        /// </summary>
        public static bool IsUniqueViolation(Exception? exception, string? constraintName = null)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is not PostgresException postgres || postgres.SqlState != UniqueViolation)
                    continue;

                return constraintName is null
                    || string.Equals(postgres.ConstraintName, constraintName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
