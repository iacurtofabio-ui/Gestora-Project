namespace GestoraWebApi.Services.Dashboard.DTOs
{
    // Risposta panoramica giornaliera
    public class DashboardGiornalieroDTO
    {
        public DateOnly Data { get; set; }

        // Contatori per stato
        public int TotalePrenotazioni { get; set; }
        public int PrenotazioniAttive { get; set; }
        public int PrenotazioniInCorso { get; set; }
        public int PrenotazioniCompletate { get; set; }
        public int PrenotazioniAnnullate { get; set; }

        // Coperti (escluse le prenotazioni annullate)
        public int TotaleCopertiPrenotati { get; set; }

        // Stato postazioni
        public int TotalePostazioniAttive { get; set; }
        public int PostazioniOccupate { get; set; }
        public int PostazioniLibere { get; set; }

        // Dettaglio per fascia oraria
        public List<CopertiFasciaDTO> CopertiPerFascia { get; set; } = new();
    }

    public class CopertiFasciaDTO
    {
        public long FasciaOrariaId { get; set; }
        public string OraInizio { get; set; } = string.Empty;
        public string OraFine { get; set; } = string.Empty;
        public int MaxCoperti { get; set; }
        public int CopertiPrenotati { get; set; }
        public int CopertiDisponibili { get; set; }
        public int NumeroPrenotazioni { get; set; }

        /// <summary>
        /// REV-039: tavoli impegnati <b>in questa fascia</b>. Il conteggio giornaliero da solo
        /// non basta: un tavolo occupato a pranzo risultava occupato anche a cena.
        /// </summary>
        public int PostazioniOccupate { get; set; }

        /// <summary>Tavoli attivi ancora liberi in questa fascia.</summary>
        public int PostazioniLibere { get; set; }
    }

    // Risposta panoramica settimanale 
    public class DashboardSettimanaleDTO
    {
        public DateOnly DataInizio { get; set; }
        public DateOnly DataFine { get; set; }

        public int TotalePrenotazioni { get; set; }
        public int TotaleCoperti { get; set; }

        /// <summary>
        /// Percentuale prenotazioni annullate sul totale del periodo.
        /// </summary>
        public double TassoAnnullamento { get; set; }

        /// <summary>
        /// Percentuale no-show: prenotazioni rimaste nello stato Attiva
        /// oltre la data di prenotazione (il cliente non si è presentato
        /// e lo staff non ha né confermato né annullato).
        /// </summary>
        public double TassoNoShow { get; set; }

        public List<GiornoSettimanaleDTO> Giorni { get; set; } = new();
    }

    public class GiornoSettimanaleDTO
    {
        public DateOnly Data { get; set; }
        public string GiornoNome { get; set; } = string.Empty;
        public int NumeroPrenotazioni { get; set; }
        public int NumeroCoperti { get; set; }
        public int Annullate { get; set; }
    }
}