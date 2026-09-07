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
    /// <b>Quale elemento della catena si prende.</b> <c>X-Forwarded-For</c> e' una lista in cui
    /// ogni proxy attraversato aggiunge in coda l'indirizzo da cui ha ricevuto la richiesta. Non
    /// va preso il primo: quello e' l'elemento che il client stesso puo' aver scritto, e
    /// leggerlo permetterebbe di falsificare l'indirizzo nell'audit trail e di aggirare il rate
    /// limit del login, che partiziona proprio su questo valore. Ma non va preso nemmeno
    /// l'ultimo, perche' qui davanti all'applicazione ci sono <b>due</b> livelli di proxy e
    /// l'ultimo anello e' il proxy di frontiera, non chi ha fatto la richiesta.
    /// </para>
    /// <para>
    /// Catena osservata in produzione il 07/09/2026:
    /// <code>
    /// X-Forwarded-For: 87.15.141.109, 79.127.178.81
    ///                  ^ client        ^ proxy di frontiera
    /// Connection.RemoteIpAddress: 100.64.0.4   (rete interna, ultimo hop)
    /// </code>
    /// Si scarta quindi <see cref="AnelliDaScartare"/> anello in fondo e si prende quello che
    /// resta per ultimo. La proprieta' di sicurezza regge: se un client inviasse una catena
    /// inventata, l'header diventerebbe <c>fake, 87.15.141.109, 79.127.178.81</c> e scartando
    /// l'ultimo si otterrebbe comunque l'indirizzo vero.
    /// </para>
    /// <para>
    /// ⚠️ <b>Se un domani la piattaforma cambia il numero di proxy, questo valore va rimisurato</b>
    /// con l'endpoint diagnostico: leggere l'header <b>prima</b> che qualcuno lo consumi. E' su
    /// questo che ho sbagliato la prima diagnosi — <c>UseForwardedHeaders</c> rimuove l'anello
    /// che elabora, quindi guardando l'header a valle sembrava esserci un solo proxy.
    /// </para>
    /// </summary>
    public static class IndirizzoClient
    {
        private const string HeaderInoltro = "X-Forwarded-For";

        /// <summary>
        /// Quanti anelli in fondo alla catena appartengono all'infrastruttura e non al client.
        /// Misurato in produzione: il proxy di frontiera ne aggiunge uno.
        /// </summary>
        private const int AnelliDaScartare = 1;

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
                // indirizzi separati da virgola: vanno appiattiti prima di contare gli anelli.
                var catena = inoltrati
                    .SelectMany(riga => (riga ?? string.Empty).Split(','))
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                // Scartati gli anelli dell'infrastruttura, l'ultimo rimasto e' il client.
                var indirizzo = catena
                    .Take(catena.Count - AnelliDaScartare)
                    .LastOrDefault();

                if (!string.IsNullOrWhiteSpace(indirizzo))
                    return Normalizza(indirizzo);

                // Catena piu' corta del previsto: manca un anello rispetto a quanto misurato.
                // Si ricade sull'indirizzo della connessione invece di restituire il proxy, che
                // sarebbe un dato sbagliato travestito da buono.
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
