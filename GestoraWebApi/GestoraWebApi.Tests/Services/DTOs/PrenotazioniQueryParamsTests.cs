using GestoraWebApi.Services.Prenotazioni.DTOs;

namespace GestoraWebApi.Tests.Services.DTOs;

/// <summary>
/// REV-019: Page arrivava dalla query string senza controlli e finiva direttamente nel calcolo
/// Skip((Page - 1) * PageSize). Con page=0 quel calcolo vale Skip(-20), che il provider rifiuta
/// a runtime: un valore fuori range in una query string - cioe' qualcosa che chiunque puo'
/// scrivere nell'indirizzo - faceva fallire la chiamata invece di riportare alla prima pagina.
/// </summary>
public class PrenotazioniQueryParamsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Page_RiportaADuno_QuandoValoreNonValido(int valore)
    {
        var query = new PrenotazioniQueryParams { Page = valore };

        Assert.Equal(1, query.Page);
    }

    [Fact]
    public void Page_ValoreValidoResta_EIlDefaultEUno()
    {
        Assert.Equal(1, new PrenotazioniQueryParams().Page);
        Assert.Equal(7, new PrenotazioniQueryParams { Page = 7 }.Page);
    }

    // Il vero effetto del fix: lo Skip calcolato non e' mai negativo, qualunque cosa arrivi.
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SkipCalcolato_NonEMaiNegativo(int pageRichiesta)
    {
        var query = new PrenotazioniQueryParams { Page = pageRichiesta, PageSize = 20 };

        var skip = (query.Page - 1) * query.PageSize;

        Assert.Equal(0, skip);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(101, 100)]
    [InlineData(50, 50)]
    public void PageSize_RestaDentroILimiti(int valore, int atteso)
    {
        var query = new PrenotazioniQueryParams { PageSize = valore };

        Assert.Equal(atteso, query.PageSize);
    }
}
