using Microsoft.OpenApi.Models;
using System.Reflection;

namespace GestoraWebApi.Extensions
{
    public static class SwaggerSetup
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SchemaFilter<TimeSpanSchemaFilter>();
                options.EnableAnnotations();

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Gestora API",
                    Version = "v1",
                    Description = "API per la gestione di prenotazioni, postazioni e fasce orarie " +
                                  "per attività commerciali (ristoranti, pub, pizzerie).\n\n" +
                                  "**Ruoli disponibili:** Admin · Staff · Cliente\n\n" +
                                  "**Formato date:** yyyy-MM-dd (es. 2026-05-04)\n\n" +
                                  "**Autenticazione:** JWT Bearer — effettua il login e incolla il token nel lucchetto.",
                    Contact = new OpenApiContact
                    {
                        Name = "Fabio Iacurto",
                        Email = "iacurto.fabio@outloo.com"
                    }
                });

                // Raggruppa gli endpoint per tag (nome controller)
                // e aggiunge il ruolo richiesto nella descrizione del tag
                options.TagActionsBy(api =>
                {
                    if (api.GroupName != null)
                        return new[] { api.GroupName };

                    var controllerName = api.ActionDescriptor.RouteValues["controller"];
                    return new[] { controllerName ?? "Other" };
                });

                options.DocInclusionPredicate((_, _) => true);

                // Ordine dei tag nella UI
                options.OrderActionsBy(api => $"{api.ActionDescriptor.RouteValues["controller"]}_{api.HttpMethod}");

                // JWT Authorization config
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Inserisci il token JWT ottenuto dal login. Non serve aggiungere 'Bearer' manualmente."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestora API v1");
                c.DocumentTitle = "Gestora API — Documentazione";
                c.DefaultModelsExpandDepth(-1); // nasconde i modelli in fondo — più pulito
                c.DisplayRequestDuration();     // mostra il tempo di risposta in ogni chiamata
            });

            return app;
        }
    }
}