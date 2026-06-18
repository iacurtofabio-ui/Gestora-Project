using GestoraWebApi.Enums;

namespace GestoraWebApi.Services.Prenotazioni.DTOs
{
    public class PrenotazioniQueryParams
    {
        private int _pageSize = 20;

        public int Page { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 100 ? 100 : value < 1 ? 1 : value;
        }

        public DateOnly? Data { get; set; }
        public StatoPrenotazione? Stato { get; set; }
    }
}
