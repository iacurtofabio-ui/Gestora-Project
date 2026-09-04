using GestoraWebApi.Context;
using Microsoft.EntityFrameworkCore;

namespace GestoraWebApi.Common
{
    /// <summary>
    /// REV-032: esegue piu' scritture come una sola operazione atomica.
    /// <para>
    /// Serve perche' ogni repository chiama <c>SaveChanges</c> per conto suo: scrivere l'entita'
    /// e poi registrarla nell'audit trail sono quindi due salvataggi distinti, e se il secondo
    /// fallisce resta a database una modifica di cui non c'e' traccia in nessun registro.
    /// Dentro una transazione i due salvataggi condividono la stessa sorte.
    /// </para>
    /// <para>
    /// La transazione va aperta dentro l'execution strategy, non attorno: con
    /// <c>EnableRetryOnFailure</c> attivo, un ritentativo deve poter rieseguire l'intero blocco
    /// dall'inizio, e non potrebbe farlo se la transazione fosse stata aperta prima.
    /// </para>
    /// <para>
    /// <c>PrenotazioniService</c> non usa questo helper: ha il proprio, che oltre alla
    /// transazione traduce la violazione dell'unique index sullo slot in un 409 leggibile
    /// (REV-003). Qui quel caso non esiste.
    /// </para>
    /// </summary>
    public interface IEsecutoreTransazione
    {
        Task EseguiAsync(Func<Task> operazione);
    }

    public sealed class EsecutoreTransazione : IEsecutoreTransazione
    {
        private readonly GestoraContext _context;

        public EsecutoreTransazione(GestoraContext context)
        {
            _context = context;
        }

        public async Task EseguiAsync(Func<Task> operazione)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                await operazione();

                await transaction.CommitAsync();
                // Nessun catch: se l'operazione fallisce, la transazione non viene mai
                // committata e il Dispose fa il rollback. L'eccezione originale deve arrivare
                // al middleware, che sa gia' tradurla nello status code giusto.
            });
        }
    }
}
