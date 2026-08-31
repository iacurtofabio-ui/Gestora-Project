using GestoraWebApi.Extensions;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace GestoraWebApi.Services.FasciaOrarie.DTOs
{
    public class FasciaOrariaDTO
    {
        public long Id { get; set; }
        public string OrarioInizio { get; set; } //passo tipo string per evitare problemi di serializzazione con TimeOnly
        public string OrarioFine { get; set; } // passo tipo string per evitare problemi di serializzazione con TimeOnly
        public DayOfWeek GiornoSettimana { get; set; }
        public int MaxCoperti { get; set; }
        public bool Attiva { get; set; }
    }
}
