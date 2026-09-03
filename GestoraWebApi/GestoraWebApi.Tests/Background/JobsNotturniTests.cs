using GestoraWebApi.Background;
using GestoraWebApi.Services.Prenotazioni;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;

namespace GestoraWebApi.Tests.Background;

/// <summary>
/// REV-054: i due job schedulati sono gusci sottili — aprono uno scope, chiamano il service e
/// non lasciano uscire eccezioni. La logica di dominio è coperta in
/// <see cref="Services.PrenotazioniServiceTests"/>; qui si verifica il guscio, in particolare
/// che un errore non esca dal job: Quartz lo tratterebbe come misfire, e con lo store
/// persistente il trigger resterebbe in errore fino al riavvio.
/// </summary>
public class JobsNotturniTests
{
    private readonly Mock<IPrenotazioniService> _prenotazioniService = new();
    private readonly IServiceProvider _provider;

    public JobsNotturniTests()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _prenotazioniService.Object);
        _provider = services.BuildServiceProvider();
    }

    private static IJobExecutionContext ContestoQuartz() => new Mock<IJobExecutionContext>().Object;

    [Fact]
    public async Task PrenotazioniJob_CompletaLePrenotazioniScadute()
    {
        var job = new PrenotazioniJob(_provider, new Mock<ILogger<PrenotazioniJob>>().Object);

        await job.Execute(ContestoQuartz());

        _prenotazioniService.Verify(s => s.AutomaticCompletPrenotazioniAsync(), Times.Once);
    }

    [Fact]
    public async Task PrenotazioniJob_NonPropagaLEccezione()
    {
        _prenotazioniService.Setup(s => s.AutomaticCompletPrenotazioniAsync())
                            .ThrowsAsync(new Exception("database non raggiungibile"));
        var job = new PrenotazioniJob(_provider, new Mock<ILogger<PrenotazioniJob>>().Object);

        await job.Execute(ContestoQuartz()); // non deve lanciare
    }

    [Fact]
    public async Task PrenotazioniCleanupJob_EliminaLoStoricoVecchio()
    {
        var job = new PrenotazioniCleanupJob(_provider, new Mock<ILogger<PrenotazioniCleanupJob>>().Object);

        await job.Execute(ContestoQuartz());

        _prenotazioniService.Verify(s => s.AutomaticDeletePrenotazioniAsync(), Times.Once);
    }

    [Fact]
    public async Task PrenotazioniCleanupJob_NonPropagaLEccezione()
    {
        _prenotazioniService.Setup(s => s.AutomaticDeletePrenotazioniAsync())
                            .ThrowsAsync(new Exception("database non raggiungibile"));
        var job = new PrenotazioniCleanupJob(_provider, new Mock<ILogger<PrenotazioniCleanupJob>>().Object);

        await job.Execute(ContestoQuartz()); // non deve lanciare
    }
}
