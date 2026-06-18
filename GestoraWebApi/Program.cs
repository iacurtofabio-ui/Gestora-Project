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



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GestoraContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Configura Quartz con persistenza su PostgreSQL
builder.Services.AddQuartz(q =>
{
    q.UsePersistentStore(store =>
    {
        store.UsePostgres(pg =>
        {
            pg.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
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

app.MapControllers();

app.Run();
