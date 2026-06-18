using FluentValidation;
using GestoraWebApi.Services.Auth.DTOs;

namespace GestoraWebApi.Validators
{
    public class RegisterDTOValidator : AbstractValidator<RegisterDTO>
    {
        public RegisterDTOValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Il nome utente è obbligatorio.")
                .MinimumLength(3).WithMessage("Il nome utente deve avere almeno 3 caratteri.")
                .MaximumLength(50).WithMessage("Il nome utente non può superare 50 caratteri.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'email è obbligatoria.")
                .EmailAddress().WithMessage("Inserire un indirizzo email valido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La password è obbligatoria.")
                .MinimumLength(8).WithMessage("La password deve avere almeno 8 caratteri.")
                .Matches("[A-Z]").WithMessage("La password deve contenere almeno una lettera maiuscola.")
                .Matches("[0-9]").WithMessage("La password deve contenere almeno un numero.")
                .Matches("[^a-zA-Z0-9]").WithMessage("La password deve contenere almeno un carattere speciale.");
        }
    }
}