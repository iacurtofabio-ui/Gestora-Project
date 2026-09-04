using Microsoft.AspNetCore.HttpOverrides;
using GestoraWebApi.Auth;
using GestoraWebApi.Background;
using GestoraWebApi.Context;
using GestoraWebApi.Extensions;
using GestoraWebApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using GestoraWebApi.Mappings;
using GestoraWebApi.Repositories.FasciaOrarie;
using GestoraWebApi.Repositories.LogActivity;
using GestoraWebApi.Repositories.Postazioni;
using GestoraWebApi.Repositories.Prenotazioni;
using GestoraWebApi.Repositories.Zone;
using GestoraWebApi.Services.Disponibilita;
using GestoraWebApi.Services.FasciaOrarie;
using GestoraWebApi.Services.LogActivity;
using GestoraWebApi.Services.Postazioni;
using GestoraWebApi.Services.PostazioneAssignment;
using GestoraWebApi.Services.Prenotazioni;
using GestoraWebApi.Services.Zone;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Quartz;
using Quartz.Impl;
using FluentValidation;
using FluentValidation.AspNetCore;
using GestoraWebApi.Services.Dashboard;
using GestoraWebApi.Infrastructure.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;



var builder = WebApplication.CreateBuilder(args);

// Configurazione obbligatoria: senza questi valori il servizio non puo funzionare.
// Si valida subito, prima di registrare qualsiasi servizio, per fallire con un messaggio
// esplicito invece che piu avanti con errori opachi del driver
// (es. "The ConnectionString property has not been initialized" dentro il seed dei ruoli).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configurazione mancante: ConnectionStrings:DefaultConnection. " +
        "In sviluppo impostarla con 'dotnet user-secrets set', in produzione come variabile " +
        "d'ambiente ConnectionStrings__DefaultConnection.");
}

var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "Configurazione mancante: JwtSettings:Secret. " +
        "In sviluppo impostarlo con 'dotnet user-secrets set', in produzione come variabile " +
        "d'ambiente JwtSettings__Secret.");
}

// HMAC-SHA256 richiede una chiave di almeno 256 bit: sotto questa soglia la generazione
// del token fallirebbe solo al primo login, non all'avvio.
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException(
        "JwtSettings:Secret troppo corto: servono almeno 32 caratteri (256 bit) per HMAC-SHA256.");
}

builder.Services.AddDbContext<GestoraContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        // Connection resiliency: la rete privata tra i container non e' immediatamente
        // disponibile all'avvio e puo avere interruzioni transitorie. Senza retry il primo
        // avvio puo fallire anche con la configurazione corretta.
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)));

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

//JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Json Serializza TimeSpan come stringa "HH:mm"
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new TimeSpanToStringConverter());
    });

// Configura Quartz con persistenza su PostgreSQL.
//
// REV-028 — ⚠️ lo scheduler NON e' in cluster mode (manca store.UseClustering()).
// Oggi non e' un problema: la piattaforma esegue una sola istanza del servizio, quindi c'e' un
// solo scheduler e ogni job parte una volta. Diventa un problema nel momento in cui le repliche
// diventano due o piu': senza clustering ogni istanza legge le stesse tabelle QRTZ_ ma non
// concorre per acquisire il trigger, quindi lo stesso job partirebbe su ognuna. Le conseguenze
// non sono simmetriche fra i due job:
//   - PrenotazioniJob completa prenotazioni gia' scadute: rieseguirlo e' innocuo, la seconda
//     passata non trova piu' nulla da fare;
//   - PrenotazioniCleanupJob cancella fisicamente righe: due esecuzioni in parallelo sugli
//     stessi Id sono una corsa fra DELETE, con errori nei log per righe gia' sparite.
// Se un giorno si aggiungono repliche, prima di farlo va abilitato il clustering
// (store.UseClustering(...) e un SchedulerId distinto per istanza). Non si anticipa qui perche'
// il clustering ha un costo — heartbeat e lock a database a ogni ciclo — che oggi si pagherebbe
// senza alcun ritorno.
builder.Services.AddQuartz(q =>
{
    q.UsePersistentStore(store =>
    {
        store.UsePostgres(pg =>
        {
            pg.ConnectionString = connectionString;
            pg.TablePrefix = "QRTZ_";
        });
        store.UseNewtonsoftJsonSerializer();
    });

    var jobKey = new JobKey("PrenotazioniJob");
    var jobCleanupKey = new JobKey("PrenotazioniCleanupJob");

    q.AddJob<PrenotazioniJob>(opts => opts.WithIdentity(jobKey).StoreDurably());
    q.AddJob<PrenotazioniCleanupJob>(opts => opts.WithIdentity(jobCleanupKey).StoreDurably());

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("PrenotazioniJob-trigger")
        .WithCronSchedule("0 00 2 * * ?"));

    q.AddTrigger(opts => opts
        .ForJob(jobCleanupKey)
        .WithIdentity("PrenotazioniCleanupJob-trigger")
        .WithCronSchedule("0 30 2 * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);



// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

//In-memory cache
builder.Services.AddMemoryCache();

// Health check: endpoint interrogato dalla piattaforma di hosting dopo ogni deploy.
// Se non risponde, il deploy viene marcato come fallito e resta online la versione precedente.
// AddDbContextCheck verifica anche che il database sia raggiungibile: senza, un deploy con DB
// giù o rete privata non ancora pronta risultava comunque "Healthy" (il check controllava solo
// che il processo rispondesse, non che potesse fare il suo lavoro).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GestoraContext>("database");

// Rate limiting sul login: senza, un brute force sulla password non ha alcun freno lato server
// (il lockout di Identity, configurato in AddJwtAuthentication, blocca l'account dopo N
// tentativi falliti; questo limita anche il volume di richieste per IP, indipendentemente
// dall'account preso di mira).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Altri servizi
builder.Services.AddSwaggerDocumentation();

builder.Services.AddAutoMapper(typeof(PostazioneMappingProfile));

builder.Services.AddHttpContextAccessor();

builder.Services.ConfigureCustomInvalidModelState();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Un solo orologio per tutto il progetto (REV-016 / REV-092)
builder.Services.AddSingleton<GestoraWebApi.Common.IClock, GestoraWebApi.Common.SystemClock>();

// REV-032: helper condiviso per eseguire scrittura + audit log in una sola transazione.
// Scoped come il DbContext: deve essere lo stesso contesto della richiesta, altrimenti la
// transazione non avvolgerebbe le scritture dei repository.
builder.Services.AddScoped<GestoraWebApi.Common.IEsecutoreTransazione, GestoraWebApi.Common.EsecutoreTransazione>();

//REPOSITORY
builder.Services.AddScoped<ILogActivityRepository, LogActivityRepository>();
builder.Services.AddScoped<IPostazioneRepository, PostazioneRepository>();
builder.Services.AddScoped<IFasciaOrariaRepository, FasciaOrariaRepository>();
builder.Services.AddScoped<IPrenotazioniRepository, PrenotazioniRepository>();
builder.Services.AddScoped<IZonaRepository, ZonaRepository>();


//SERVICES
builder.Services.AddScoped<ILogActivityService, LogActivityService>();
builder.Services.AddScoped<IPostazioneService, PostazioneService>();
builder.Services.AddScoped<IFasciaOrariaService, FasciaOrariaService>();
builder.Services.AddScoped<IPrenotazioniService, PrenotazioniService>();
builder.Services.AddScoped<IPostazioneAssignmentService, PostazioneAssignmentService>();
builder.Services.AddScoped<IDisponibilitaService, DisponibilitaService>();
builder.Services.AddScoped<IZonaService, ZonaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// Seed ruoli al primo avvio (Admin, Staff, Cliente)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(roleManager);
}

// Avviso esplicito se il database non è allineato al codice. Le migration restano applicate a
// mano (decisione di progetto), quindi questo non blocca l'avvio: serve solo a non scoprire il
// disallineamento dal primo errore su un endpoint qualsiasi, che sarebbe molto meno leggibile.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GestoraContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
        {
            startupLogger.LogWarning(
                "ATTENZIONE: il database ha {Count} migration non applicate: {Migrations}. " +
                "Il codice si aspetta uno schema più recente di quello presente — applicare con " +
                "'dotnet ef database update' prima di considerare l'ambiente allineato.",
                pending.Count, string.Join(", ", pending));
        }
    }
    catch (Exception ex)
    {
        // Non deve mai impedire l'avvio: se il controllo stesso fallisce (es. DB
        // temporaneamente irraggiungibile), meglio un avviso in log che un crash.
        startupLogger.LogWarning(ex, "Impossibile verificare l'allineamento delle migration all'avvio.");
    }
}

// REV-029: l'applicazione gira dietro il proxy della piattaforma, quindi senza questo middleware
// Connection.RemoteIpAddress e' l'indirizzo del proxy, uguale per chiunque. Due conseguenze,
// entrambe presenti fino a oggi:
//   1. l'audit trail registrava sempre lo stesso indirizzo, cioe' non registrava nulla di utile;
//   2. il rate limit del login (LoginPolicy) partiziona proprio su quell'indirizzo: era di fatto
//      un limite globale di 5 tentativi al minuto per l'intera applicazione, che invece di
//      fermare chi attacca poteva bloccare gli utenti legittimi.
//
// Va registrato per primo: tutto cio' che viene dopo deve gia' vedere l'indirizzo corretto.
//
// KnownProxies/KnownNetworks vanno svuotati perche' il proxy non e' su loopback e il suo
// indirizzo non e' noto in anticipo; senza questo l'header verrebbe semplicemente ignorato.
// ForwardLimit = 1 e' cio' che rende la cosa sicura: si prende solo l'ultimo anello della
// catena X-Forwarded-For, quello scritto dal proxy della piattaforma. Un client che si
// inventasse l'header lo vedrebbe scavalcato dal valore aggiunto dal proxy, quindi non puo'
// spacciarsi per un altro indirizzo per aggirare il rate limit.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeaders);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerDocumentation();
}

app.UseGlobalExceptionHandler();

app.UseCors("FrontendPolicy");

//app.UseHttpsRedirection(); (verificare se serve abilitare in produzione)

app.UseAuthentication();

app.UseAuthorization();

app.UseRateLimiter();

// Endpoint pubblico e senza autenticazione: deve poter rispondere anche a chi non ha un token.
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
