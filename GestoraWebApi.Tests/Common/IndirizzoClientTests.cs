using System.Net;
using GestoraWebApi.Common;
using Microsoft.AspNetCore.Http;

namespace GestoraWebApi.Tests.Common;

/// <summary>
/// REV-029. Questi test esistono perché la prima soluzione — <c>UseForwardedHeaders</c> — non era
/// verificabile: il middleware non si può interrogare, in locale gli header di inoltro non
/// esistono e ogni ipotesi andava provata con un deploy in produzione. Ne sono serviti tre.
/// La lettura esplicita si può invece esercitare qui, in millisecondi.
/// </summary>
public class IndirizzoClientTests
{
    private static HttpContext Richiesta(string? remoteIp = "10.0.0.5", params string[] forwardedFor)
    {
        var context = new DefaultHttpContext();

        if (remoteIp is not null)
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);

        if (forwardedFor.Length > 0)
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        return context;
    }

    [Fact]
    public void SenzaHeader_UsaLIndirizzoDellaConnessione()
    {
        // È il caso dello sviluppo in locale: nessun proxy davanti, l'indirizzo della
        // connessione è già quello giusto.
        Assert.Equal("10.0.0.5", IndirizzoClient.Ottieni(Richiesta()));
    }

    // La catena esatta osservata in produzione il 07/09/2026: il client, poi il proxy di
    // frontiera che aggiunge il proprio anello. La connessione arriva dalla rete interna.
    [Fact]
    public void CatenaRealeDiProduzione_RestituisceIlClient()
    {
        var context = Richiesta("100.64.0.4", "87.15.141.109, 79.127.178.81");

        Assert.Equal("87.15.141.109", IndirizzoClient.Ottieni(context));
    }

    // Se la catena e' piu' corta del previsto manca un anello rispetto a quanto misurato:
    // restituire l'unico valore presente significherebbe registrare il proxy come se fosse il
    // client, cioe' un dato sbagliato travestito da buono.
    [Fact]
    public void ConUnSoloAnello_RicadeSullaConnessione()
    {
        Assert.Equal("10.0.0.5", IndirizzoClient.Ottieni(Richiesta("10.0.0.5", "79.127.178.81")));
    }

    // Il punto di sicurezza dell'intera soluzione: il primo elemento della catena può essere
    // stato scritto dal client, l'ultimo no. Prendendo l'ultimo, un client che si inventa
    // l'header non riesce a falsificare l'indirizzo nell'audit trail né ad aggirare il rate
    // limit del login, che partiziona su questo valore.
    [Fact]
    public void ConCatenaFalsificataDalClient_IgnoraIlValoreInventato()
    {
        // Il client invia "1.2.3.4" sperando di farsi registrare con quell'indirizzo. Gli anelli
        // veri vengono aggiunti dopo, quindi scartando quello dell'infrastruttura si ottiene
        // comunque il suo indirizzo reale.
        var context = Richiesta("100.64.0.4", "1.2.3.4, 87.15.141.109, 79.127.178.81");

        Assert.Equal("87.15.141.109", IndirizzoClient.Ottieni(context));
    }

    [Fact]
    public void ScartaSoloLAnelloDellInfrastruttura_NonDiPiu()
    {
        var context = Richiesta("10.0.0.1", "87.15.141.109, 203.0.113.7, 198.51.100.4");

        Assert.Equal("203.0.113.7", IndirizzoClient.Ottieni(context));
    }

    // L'header può arrivare come più righe distinte invece che come una sola lista.
    [Fact]
    public void ConHeaderSuPiuRighe_LeAppiattiscePrimaDiContareGliAnelli()
    {
        var context = Richiesta("10.0.0.1", "1.2.3.4", "87.15.141.109, 203.0.113.7");

        Assert.Equal("87.15.141.109", IndirizzoClient.Ottieni(context));
    }

    [Fact]
    public void IgnoraGliSpaziEIValoriVuoti()
    {
        var context = Richiesta("10.0.0.1", "  1.2.3.4 ,  87.15.141.109  , 79.127.178.81 , ");

        Assert.Equal("87.15.141.109", IndirizzoClient.Ottieni(context));
    }

    [Fact]
    public void HeaderPresenteMaVuoto_RicadeSullaConnessione()
    {
        var context = Richiesta("10.0.0.5", "");

        Assert.Equal("10.0.0.5", IndirizzoClient.Ottieni(context));
    }

    // Alcuni proxy scrivono anche la porta: nell'audit trail deve finire il solo indirizzo,
    // altrimenti la stessa persona comparirebbe con valori diversi a ogni richiesta e il rate
    // limit la conterebbe come client sempre nuovo.
    [Fact]
    public void ConPortaSuIPv4_TieneSoloLIndirizzo()
    {
        Assert.Equal("87.15.141.109", IndirizzoClient.Ottieni(Richiesta("10.0.0.1", "87.15.141.109:54321, 79.127.178.81")));
    }

    [Fact]
    public void ConIPv6TraParentesiEPorta_TieneSoloLIndirizzo()
    {
        Assert.Equal("2001:db8::1", IndirizzoClient.Ottieni(Richiesta("10.0.0.1", "[2001:db8::1]:54321, 79.127.178.81")));
    }

    // Un IPv6 nudo contiene due punti ovunque: non va scambiato per "indirizzo con porta".
    [Fact]
    public void ConIPv6SenzaPorta_RestaIntatto()
    {
        Assert.Equal("2001:db8::1", IndirizzoClient.Ottieni(Richiesta("10.0.0.1", "2001:db8::1, 79.127.178.81")));
    }

    [Fact]
    public void SenzaContesto_RestituisceNull()
    {
        Assert.Null(IndirizzoClient.Ottieni(null));
    }

    [Fact]
    public void SenzaHeaderESenzaConnessione_RestituisceNull()
    {
        Assert.Null(IndirizzoClient.Ottieni(Richiesta(remoteIp: null)));
    }
}
