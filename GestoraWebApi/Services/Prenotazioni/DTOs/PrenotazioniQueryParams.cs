using GestoraWebApi.Enums;

namespace GestoraWebApi.Services.Prenotazioni.DTOs
{
    public class PrenotazioniQueryParams
    {
        private int _page = 1;
        private int _pageSize = 20;

        // REV-019: Page arrivava dalla query string senza alcun controllo. Con ?page=0 il
        // calcolo (Page - 1) * PageSize produceva Skip(-20), che a runtime fa fallire la
        // query invece di restituire la prima pagina. Come per PageSize, il valore fuori
        // range viene riportato dentro i limiti invece di generare un errore: e' un
        // parametro di navigazione, non un dato di dominio.
        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 100 ? 100 : value < 1 ? 1 : value;
        }

        public DateOnly? Data { get; set; }
        public StatoPrenotazione? Stato { get; set; }
    }
}
