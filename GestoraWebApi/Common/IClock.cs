namespace GestoraWebApi.Common
{
    /// <summary>
    /// Un solo orologio per tutto il progetto (REV-016 / REV-092). Il database e la logica
    /// interna lavorano in UTC; la conversione a ora italiana avviene solo al confine
    /// (validator, dashboard, frontend) tramite <see cref="NowInRome"/> / <see cref="TodayInRome"/>.
    /// Astratto così da poter essere sostituito nei test con un orologio fisso.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
        DateTime NowInRome { get; }
        DateOnly TodayInRome { get; }
    }

    public sealed class SystemClock : IClock
    {
        private static readonly TimeZoneInfo RomeTimeZone = ResolveRomeTimeZone();

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime NowInRome => TimeZoneInfo.ConvertTime(DateTime.UtcNow, RomeTimeZone);

        public DateOnly TodayInRome => DateOnly.FromDateTime(NowInRome);

        private static TimeZoneInfo ResolveRomeTimeZone()
        {
            // "Europe/Rome" è l'ID IANA (Linux/container, e Windows via ICU su .NET moderno);
            // "W. Europe Standard Time" è il fallback per ambienti Windows senza ICU.
            foreach (var id in new[] { "Europe/Rome", "W. Europe Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
