using System.Globalization;
using GestoraWebApi.Models;

namespace GestoraWebApi.Common
{
    /// <summary>
    /// Testo per i messaggi di blocco (tavoli e zone): quale prenotazione impedisce la modifica.
    /// </summary>
    public static class DescrizioneImpegni
    {
        private static readonly CultureInfo Italiano = new("it-IT");

        /// <summary>"alla prenotazione di sabato 10/10/2026, 20:00-22:00 (4 persone) e ad altre 2".</summary>
        public static string Descrivi(IReadOnlyList<Prenotazione> impegni)
        {
            var prima = impegni[0];
            var giorno = Italiano.DateTimeFormat.GetDayName(prima.DataPrenotazione.DayOfWeek);
            var fascia = prima.FasciaOraria == null
                ? ""
                : $", {prima.FasciaOraria.OrarioInizio:HH\\:mm}-{prima.FasciaOraria.OrarioFine:HH\\:mm}";
            var altre = impegni.Count > 1 ? $" e ad altre {impegni.Count - 1}" : "";

            return $"alla prenotazione di {giorno} {prima.DataPrenotazione.ToString("dd/MM/yyyy", Italiano)}{fascia} " +
                   $"({prima.NumeroCoperti} persone){altre}";
        }
    }
}
