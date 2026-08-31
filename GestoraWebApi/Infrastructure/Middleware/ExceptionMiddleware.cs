using GestoraWebApi.Infrastructure.Exceptions;
using GestoraWebApi.Models;
using System.Net;
using System.Text.Json;
using ValidationException = GestoraWebApi.Infrastructure.Exceptions.ValidationException;

namespace GestoraWebApi.Infrastructure.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled Exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                ValidationException => (int)HttpStatusCode.BadRequest,
                ArgumentOutOfRangeException => (int)HttpStatusCode.BadRequest,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.Conflict,
                _ => (int)HttpStatusCode.InternalServerError
            };

            // Per ValidationException esponiamo gli errori per campo
            List<ErrorItem>? errors = null;
            if (exception is ValidationException validationEx && validationEx.Errors.Any())
            {
                errors = validationEx.Errors
                    .SelectMany(kvp => kvp.Value.Select(msg => new ErrorItem
                    {
                        Field = kvp.Key,
                        Error = msg
                    }))
                    .ToList();
            }

            // Per le eccezioni non gestite (500) il messaggio non va esposto al client: può
            // contenere dettagli interni del driver (es. Npgsql) o della query. Per le eccezioni
            // tipizzate (404/400/409/401) il messaggio è invece scritto apposta dal service per
            // l'utente, va restituito così com'è.
            var isUnhandled = statusCode == (int)HttpStatusCode.InternalServerError;
            var response = new ErrorDetails
            {
                StatusCode = statusCode,
                Message = isUnhandled ? "Si è verificato un errore interno del server." : exception.Message,
                Errors = errors,
                Details = _env.IsDevelopment() ? exception.StackTrace : null
            };

            context.Response.StatusCode = statusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}