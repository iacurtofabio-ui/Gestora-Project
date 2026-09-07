using Microsoft.AspNetCore.Http;

namespace GestoraWebApi.Common
{
    /// <summary>
    /// REV-029: ricava l'indirizzo di chi ha fatto la richiesta, tenendo conto del proxy che sta
    /// davanti all'applicazione in produzione.
    /// <para>
    /// <b>Perche' non si usa <c>UseForwardedHeaders</c>.</b> Il middleware standard e' stato
    /// provato per primo ed e' la scelta idiomatica, ma in questo ambiente non elabora l'header:
    /// verificato in produzione con un endpoint diagnostico, <c>X-Forwarded-For</c> arrivava
    /// valorizzato, <c>X-Original-Forwarded-For</c> restava vuoto (segno che il middleware non
    /// aveva consumato nulla) e <c>Connection.RemoteIpAddress</c> continuava a essere quello del
    /// proxy. Sono stati esclusi i sospetti sulle liste di proxy noti (svuotate, quindi il
    /// controllo e' disattivato), sull'ordine nella pipeline (e' il primo middleware) e sulla
    /// presenza di <c>X-Forwarded-Proto</c>. Invece di continuare a indagare su un componente che
    /// non e' ispezionabile dall'esterno, la lettura e' fatta qui: e' esplicita, non dipende da
    /// configurazioni implicite ed e' coperta da test.
    /// </para>
    /// <para>
    /// <b>Perche' si prende l'ultimo valore e non il primo.</b> <c>X-Forwarded-For</c> e' una
    /// lista in cui ogni proxy attraversato aggiunge in coda l'indirizzo da cui ha ricevuto la
    /// richiesta. Il primo elemento e' quello piu' vicino al client, ma e' anche quello che il
    /// client stesso puo' aver scritto: chiunque puo' inviare un <c>X-Forwarded-For</c>
    /// inventato, e leggendolo si permetterebbe di falsificare l'indirizzo nell'audit trail e di
    /// aggirare il rate limit del login, che partiziona proprio su questo valore. L'ultimo
    /// elemento e' invece scritto dal proxy della piattaforma, che non e' aggirabile.
    /// Con un solo proxy davanti — la situazione attuale, verificata — l'ultimo elemento e'
    /// esattamente l'indirizzo del client.
    /// </para>
    /// </summary>
    public static class IndirizzoClient
    {
        private const string HeaderInoltro = "X-Forwarded-For";

        /// <summary>
        /// Indirizzo del chiamante, o <c>null</c> se non determinabile.
        /// In locale, dove non c'e' alcun proxy e l'header non esiste, ricade su
        /// <c>Connection.RemoteIpAddress</c>, che li' e' gia' il valore corretto.
        /// </summary>
        public static string? Ottieni(HttpContext? context)
        {
            if (context is null)
                return null;

            if (context.Request.Headers.TryGetValue(HeaderInoltro, out var inoltrati))
            {
                // L'header puo' arrivare come piu' righe, e ogni riga puo' contenere piu'
                // indirizzi separati da virgola: vanno appiattiti prima di prendere l'ultimo.
                var indirizzo = inoltrati
                    .SelectMany(riga => (riga ?? string.Empty).Split(','))
                    .Select(v => v.Trim())
                    .LastOrDefault(v => !string.IsNullOrWhiteSpace(v));

                if (!string.IsNullOrWhiteSpace(indirizzo))
                    return Normalizza(indirizzo);
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Toglie l'eventuale porta. Alcuni proxy scrivono "1.2.3.4:5678"; per IPv6 la forma con
        /// porta e' "[::1]:5678", mentre un IPv6 nudo contiene due punti ovunque e non va toccato.
        /// </summary>
        private static string Normalizza(string indirizzo)
        {
            if (indirizzo.StartsWith('['))
            {
                var chiusura = indirizzo.IndexOf(']');
                if (chiusura > 0)
                    return indirizzo[1..chiusura];
            }

            // Un solo ':' significa IPv4 con porta. Piu' di uno significa IPv6 senza porta.
            var separatore = indirizzo.IndexOf(':');
            if (separatore > 0 && indirizzo.IndexOf(':', separatore + 1) < 0)
                return indirizzo[..separatore];

            return indirizzo;
        }
    }
}
