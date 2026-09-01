using FluentValidation;
using GestoraWebApi.Common;
using GestoraWebApi.Services.Prenotazioni.DTOs;

namespace GestoraWebApi.Validators
{
    public class PrenotazioneCreateDTOValidator : AbstractValidator<PrenotazioneCreateDTO>
    {
        public PrenotazioneCreateDTOValidator(IClock clock)
        {
            RuleFor(x => x.NumeroCoperti)
                .GreaterThan(0).WithMessage("Il numero di coperti deve essere maggiore di zero.")
                .LessThanOrEqualTo(50).WithMessage("Il numero di coperti non può superare 50.");

            RuleFor(x => x.DataPrenotazione)
                .Must(d => d >= clock.TodayInRome)
                .WithMessage("Non è possibile effettuare prenotazioni in una data passata.");

            RuleFor(x => x.FasciaOrariaId)
                .GreaterThan(0).WithMessage("Specificare una fascia oraria valida.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Le note non possono superare 500 caratteri.")
                .When(x => x.Note != null);
        }
    }
}
