using FluentValidation;
using GestoraWebApi.Common;
using GestoraWebApi.Services.Prenotazioni.DTOs;
using Microsoft.Extensions.Options;

namespace GestoraWebApi.Validators
{
    public class PrenotazioneCreateDTOValidator : AbstractValidator<PrenotazioneCreateDTO>
    {
        public PrenotazioneCreateDTOValidator(IClock clock, IOptions<PrenotazioniSettings> impostazioni)
        {
            var maxCoperti = impostazioni.Value.MaxCopertiPerPrenotazione;

            RuleFor(x => x.NumeroCoperti)
                .GreaterThan(0).WithMessage("Il numero di coperti deve essere maggiore di zero.")
                .LessThanOrEqualTo(maxCoperti).WithMessage($"Il numero di coperti non può superare {maxCoperti}.");

            RuleFor(x => x.DataPrenotazione)
                .Must(d => d >= clock.TodayInRome)
                .WithMessage("Non è possibile effettuare prenotazioni in una data passata.")
                // Stesso orizzonte di check-disponibilita: oltre, la verifica dei posti falliva
                // mentre la prenotazione passava, e nel form le fasce sembravano tutte libere.
                .Must(d => d <= clock.TodayInRome.AddDays(CheckDisponibilitaDTOValidator.GiorniMassimiInAvanti))
                .WithMessage($"Non è possibile prenotare oltre {CheckDisponibilitaDTOValidator.GiorniMassimiInAvanti} giorni.");

            RuleFor(x => x.FasciaOrariaId)
                .GreaterThan(0).WithMessage("Specificare una fascia oraria valida.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Le note non possono superare 500 caratteri.")
                .When(x => x.Note != null);
        }
    }
}
