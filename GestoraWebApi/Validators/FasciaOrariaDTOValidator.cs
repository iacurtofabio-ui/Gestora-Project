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

            // Una fascia vive dentro un solo giorno: tutto il dominio calcola la fine come
            // data + OrarioFine. Una fine "00:00" (o comunque prima dell'inizio) cadrebbe
            // all'inizio del giorno: la fascia risulterebbe gia' passata per tutta la giornata
            // e il job notturno segnerebbe "Non presentata" le prenotazioni della sera stessa.
            RuleFor(x => x)
                .Must(x => TimeSpan.Parse(x.OrarioFine) > TimeSpan.Parse(x.OrarioInizio))
                .When(x => BeValidTime(x.OrarioInizio) && BeValidTime(x.OrarioFine))
                .OverridePropertyName(nameof(FasciaOrariaDTO.OrarioFine))
                .WithMessage("L'orario di fine deve essere successivo a quello di inizio (le fasce non possono superare la mezzanotte: usa al massimo 23:59).");

            RuleFor(x => x.MaxCoperti)
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
