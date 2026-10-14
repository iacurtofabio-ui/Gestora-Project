using FluentValidation;
using GestoraWebApi.Services.Auth.DTOs;

namespace GestoraWebApi.Validators
{
    public class ConfermaEmailDTOValidator : AbstractValidator<ConfermaEmailDTO>
    {
        public ConfermaEmailDTOValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("Il link di conferma non è valido.");
            RuleFor(x => x.Token).NotEmpty().WithMessage("Il link di conferma non è valido.");
        }
    }

    public class ReinviaConfermaDTOValidator : AbstractValidator<ReinviaConfermaDTO>
    {
        public ReinviaConfermaDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'email è obbligatoria.")
                .EmailAddress().WithMessage("Inserire un indirizzo email valido.");
        }
    }
}
