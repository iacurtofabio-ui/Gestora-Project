using GestoraWebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestoraWebApi.Extensions
{
    public static class ApiBehaviorExtensions
    {
        public static IServiceCollection ConfigureCustomInvalidModelState(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value!.Errors.Count > 0)
                        .SelectMany(e => e.Value!.Errors.Select(err => new ErrorItem
                        {
                            Field = e.Key,
                            Error = err.ErrorMessage
                        }))
                        .ToList();

                    var response = new ErrorDetails
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Errore nei dati inviati",
                        Errors = errors
                    };

                    return new BadRequestObjectResult(response);
                };
            });

            return services;
        }
    }
}