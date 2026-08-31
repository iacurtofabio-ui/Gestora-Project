using GestoraWebApi.Models;

namespace GestoraWebApi.Services.PostazioneAssignment
{
    /// <summary>
    /// Esito dell'assegnazione per un singolo tavolo: il tavolo e quanti dei coperti
    /// richiesti ci siedono realmente.
    /// </summary>
    public record PostazioneAssegnata(Postazione Postazione, int PostiOccupati);
}
