using GestoraWebApi.Common;

namespace GestoraWebApi.Tests;

/// <summary>
/// Esegue l'operazione senza aprire nulla (REV-032).
/// <para>
/// I test di questi service lavorano su repository mockati, dove non esiste un database e
/// quindi nemmeno una transazione da aprire: quello che va verificato qui e' che scrittura e
/// audit log vengano richiesti insieme, non il comportamento transazionale in se'. Quello
/// dipende da Postgres e, come per l'unique index sullo slot (Fase 3), non e' riproducibile con
/// il provider InMemory.
/// </para>
/// </summary>
public sealed class EsecutoreTransazioneFinto : IEsecutoreTransazione
{
    /// <summary>Quante volte e' stata aperta un'operazione atomica.</summary>
    public int Chiamate { get; private set; }

    /// <summary>
    /// Se false l'operazione non viene eseguita affatto. Serve a dimostrare che cosa sta
    /// davvero dentro il blocco atomico: quello che non parte, era dentro.
    /// </summary>
    public bool Esegui { get; set; } = true;

    public Task EseguiAsync(Func<Task> operazione)
    {
        Chiamate++;
        return Esegui ? operazione() : Task.CompletedTask;
    }
}
