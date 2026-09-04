using FluentValidation;
using GestoraWebApi.Common;
using GestoraWebApi.Services.PrenotazioniPostazioni;

namespace GestoraWebApi.Validators
{
    /// <summary>
    /// REV-027: check-disponibilita e' l'unico endpoint dell'API raggiungibile senza
    /// autenticazione, ed era anche l'unico DTO in ingresso senza validator. Qualunque valore
    /// arrivava fino al service: una data del 1900 o coperti negativi producevano una query
    /// completa sul database e una risposta priva di senso invece di un 400 immediato.
    /// I limiti sono gli stessi di <see cref="PrenotazioneCreateDTOValidator"/>: chi chiede la
    /// disponibilita' e chi poi prenota devono ricevere la stessa risposta sugli stessi valori,
    /// altrimenti si vedrebbe una fascia disponibile che al momento di prenotare viene rifiutata.
    /// </summary>
    public class CheckDisponibilitaDTOValidator : AbstractValidator<CheckDisponibilitaDTO>
    {
        // Orizzonte massimo di prenotazione. Serve soprattutto qui: l'endpoint e' pubblico e
        // senza un tetto si potrebbero interrogare date arbitrariamente lontane, ognuna con il
        // suo giro di query, senza alcun costo per chi chiama.
        private const int GiorniMassimiInAvanti = 365;

        public CheckDisponibilitaDTOValidator(IClock clock)
        {
            RuleFor(x => x.DataPrenotazione)
                .Must(d => d >= clock.TodayInRome)
                .WithMessage("Non è possibile verificare la disponibilità per una data passata.")
                .Must(d => d <= clock.TodayInRome.AddDays(GiorniMassimiInAvanti))
                .WithMessage($"Non è possibile verificare la disponibilità oltre {GiorniMassimiInAvanti} giorni.");

            RuleFor(x => x.NumeroCoperti)
                .GreaterThan(0).WithMessage("Il numero di coperti deve essere maggiore di zero.")
                .LessThanOrEqualTo(50).WithMessage("Il numero di coperti non può superare 50.");
        }
    }
}
