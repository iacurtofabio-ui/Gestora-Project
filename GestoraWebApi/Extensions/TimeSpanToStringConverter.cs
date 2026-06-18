using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestoraWebApi.Extensions
{
    public class TimeSpanToStringConverter: JsonConverter<TimeSpan>
    {

        private static readonly string[] AllowedFormats = { @"hh\:mm", @"hh\.mm", @"hh\,mm"}; // solo formati con separatore

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("L'orario non può essere vuoto. Usa il formato hh:mm (es. 08:30)");
            }

            // Prova entrambi i formati con separatore
            foreach (var format in AllowedFormats)
            {
                if (TimeSpan.TryParseExact(value, format, null, out var result))
                {
                    return result;
                }

                // Gestione del formato HHmm senza separatore (es. 2000 -> 20:00)
                if (value.Length == 4 && int.TryParse(value, out int hhmm))
                {
                    int hours = hhmm / 100;
                    int minutes = hhmm % 100;
                    if (hours >= 0 && hours < 24 && minutes >= 0 && minutes < 60)
                        return new TimeSpan(hours, minutes, 0);
                }
            }
            // Se nessun formato valido
            throw new JsonException($"Formato ora non valido: '{value}'. Usa hh:mm (es. 08:30).");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(@"hh\:mm")); // Normalizziamo sempre con :
        }
    }
}
