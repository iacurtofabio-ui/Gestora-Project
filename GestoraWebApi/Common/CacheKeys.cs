namespace GestoraWebApi.Common
{
    public static class CacheKeys
    {
        public const string ZoneAll = "zone_all";
        public const string ZoneAttive = "zone_attive";
        public const string FasceAttive = "fasce_attive";
        public const string FascePerGiorno = "fasce_giorno_";   // + (int)DayOfWeek
        public const string PostazioniAttive = "postazioni_attive";
    }
}