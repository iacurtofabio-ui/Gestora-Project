namespace GestoraWebApi.Services.LogActivity.DTOs
{
    /// <summary>Una riga dell'audit trail, come la vede l'Admin.</summary>
    public class LogActivityDTO
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Nome dell'utente che ha compiuto l'azione. Puo' essere null: l'audit trail sopravvive
        /// all'utente, e un id che non corrisponde piu' a nessuno resta comunque una traccia
        /// valida di cosa e' successo.
        /// </summary>
        public string? UserName { get; set; }

        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IPAddress { get; set; }
    }

    /// <summary>
    /// Filtri di lettura dell'audit trail (REV-037). Stessa forma di
    /// <c>PrenotazioniQueryParams</c>: i valori fuori range vengono riportati dentro i limiti
    /// invece di far fallire la richiesta.
    /// </summary>
    public class LogActivityQueryParams
    {
        private int _page = 1;
        private int _pageSize = 50;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > 200 ? 200 : value < 1 ? 1 : value;
        }

        /// <summary>Filtra sugli eventi di un singolo utente.</summary>
        public string? UserId { get; set; }

        /// <summary>Estremo inferiore incluso (UTC).</summary>
        public DateTime? Da { get; set; }

        /// <summary>Estremo superiore incluso (UTC).</summary>
        public DateTime? A { get; set; }

        /// <summary>Ricerca libera sul testo dell'azione.</summary>
        public string? Azione { get; set; }
    }
}
