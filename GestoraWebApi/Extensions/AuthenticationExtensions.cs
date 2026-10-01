using GestoraWebApi.Auth;
using GestoraWebApi.Context;
using GestoraWebApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GestoraWebApi.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            //Configuration Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = true;

                // Il login cerca l'utente per email (FindByEmailAsync): con due account sulla
                // stessa email solleva un'eccezione e quel login darebbe 500 per sempre. Il
                // default di Identity e' false, e chiunque puo' registrarsi.
                options.User.RequireUniqueEmail = true;

                // Lockout: senza questo, CheckPasswordSignInAsync tiene traccia dei tentativi
                // falliti ma non blocca mai l'account — brute force senza freni sul login.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<GestoraContext>()
            .AddDefaultTokenProviders();

            //Configuration JWT            
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    // FASE 10: senza questo, .NET applica una tolleranza di 5 minuti oltre la
                    // scadenza dichiarata (il default della libreria, pensato per orologi di
                    // server leggermente disallineati). Un token con ExpiryMinutes=60 restava
                    // quindi valido fino a 65 minuti. Qui l'orologio e' lo stesso server che lo
                    // ha emesso, non serve tolleranza: la scadenza deve essere quella vera.
                    ClockSkew = TimeSpan.Zero
                };

                // Personalizzazione risposta 401
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        // Impedisce la risposta di default (WWW-Authenticate senza body)
                        context.HandleResponse();

                        //Imposta lo status e il content-type
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        //Scrive il body JSON
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "Devi effettuare il login per poter creare una prenotazione."
                        });

                        return context.Response.WriteAsync(result);
                    }
                };
            });

            return services;
        }
    }
}
