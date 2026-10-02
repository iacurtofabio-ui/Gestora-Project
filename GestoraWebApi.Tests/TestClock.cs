using GestoraWebApi.Common;

namespace GestoraWebApi.Tests;

/// <summary>Orologio a istante fisso per i test. Default: "adesso" reale al momento della creazione.</summary>
public sealed class TestClock : IClock
{
    private static readonly TimeZoneInfo RomeTimeZone = ResolveRome();

    public TestClock(DateTime? utcNow = null) => UtcNow = utcNow ?? DateTime.UtcNow;

    public DateTime UtcNow { get; set; }
    public DateTime NowInRome => TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(UtcNow, DateTimeKind.Utc), RomeTimeZone);
    public DateOnly TodayInRome => DateOnly.FromDateTime(NowInRome);

    private static TimeZoneInfo ResolveRome()
    {
        foreach (var id in new[] { "Europe/Rome", "W. Europe Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
