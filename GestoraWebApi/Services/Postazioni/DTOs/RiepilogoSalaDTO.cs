namespace GestoraWebApi.Services.Postazioni.DTOs
{
    /// <summary>
    /// Quadro d'insieme della sala per la pagina Postazioni (decisione 9 della roadmap):
    /// solo informativo, non vincola nulla.
    /// </summary>
    public class RiepilogoSalaDTO
    {
        public int TavoliAttivi { get; set; }
        public int PostiTotali { get; set; }
        public List<RiepilogoFasciaDTO> Fasce { get; set; } = new();
    }

    public class RiepilogoFasciaDTO
    {
        public long FasciaOrariaId { get; set; }
        public string GiornoSettimana { get; set; } = string.Empty;
        public TimeOnly OrarioInizio { get; set; }
        public TimeOnly OrarioFine { get; set; }
        public int MaxCoperti { get; set; }

        // Somma delle capienze dei tavoli attivi. Metrica informativa: non tiene conto del
        // bonus testate, che dipende da come i tavoli vengono effettivamente uniti.
        public int PostiTavoli { get; set; }

        // true se i tavoli, sulla carta, coprono il tetto dichiarato della fascia.
        public bool TettoCoperto { get; set; }
    }
}
