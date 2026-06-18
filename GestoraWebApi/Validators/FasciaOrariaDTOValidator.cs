using FluentValidation;
using GestoraWebApi.Services.FasciaOrarie.DTOs;

namespace GestoraWebApi.Validators
{
    public class FasciaOrariaDTOValidator : AbstractValidator<FasciaOrariaDTO>
    {
        public FasciaOrariaDTOValidator()
        {
            RuleFor(x => x.OrarioInizio)
                .NotEmpty().WithMessage("L'orario di inizio è obbligatorio.")
                .Matches(@"^\d{2}:\d{2}$").WithMessage("Formato orario non valido. Usa HH:mm (es. 08:30).")
                .Must(BeValidTime).WithMessage("Orario di inizio non valido. Inserire un orario tra 00:00 e 23:59.");

            RuleFor(x => x.OrarioFine)
                .NotEmpty().WithMessage("L'orario di fine è obbligatorio.")
                .Matches(@"^\d{2}:\d{2}$").WithMessage("Formato orario non valido. Usa HH:mm (es. 08:30).")
                .Must(BeValidTime).WithMessage("Orario di fine non valido. Inserire un orario tra 00:00 e 23:59.");

            RuleFor(x => x.MaxPrenotazioni)
                .GreaterThan(0).WithMessage("Il numero massimo di prenotazioni deve essere maggiore di zero.");

            RuleFor(x => x.GiornoSettimana)
                .IsInEnum().WithMessage("Il valore di GiornoSettimana non è valido (0=Domenica, 6=Sabato).");
        }

        private static bool BeValidTime(string time)
        {
            if (!TimeSpan.TryParse(time, out var ts)) return false;
            return ts >= TimeSpan.Zero && ts < TimeSpan.FromHours(24);
        }
    }
}
