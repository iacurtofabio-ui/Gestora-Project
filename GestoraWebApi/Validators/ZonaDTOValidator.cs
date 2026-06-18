using FluentValidation;
using GestoraWebApi.Services.Zone.DTOs;

namespace GestoraWebApi.Validators
{
    public class ZonaDTOValidator : AbstractValidator<ZonaDTO>
    {
        public ZonaDTOValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Il campo Nome è obbligatorio.")
                .MaximumLength(100).WithMessage("Il nome della zona non può superare 100 caratteri.");
        }
    }
}
