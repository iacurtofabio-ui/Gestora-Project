using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GestoraWebApi.Extensions
{
    public class TimeSpanSchemaFilter : ISchemaFilter
    {
        public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(TimeSpan) && schema is OpenApiSchema openApiSchema)
            {
                openApiSchema.Examples = [JsonValue.Create("08:30")];
            }
        }
    }
}
