using FluentValidation;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Validators
{
    public class PostazioneDTOValidator : AbstractValidator<PostazioneDTO>
    {
        public PostazioneDTOValidator()
        {
            RuleFor(x => x.Numero)
                .GreaterThan(0).WithMessage("Specificare un numero valido per la postazione.");

            RuleFor(x => x.CapienzaMassima)
                .GreaterThan(0).WithMessage("La capienza deve essere almeno 1 posto.");

            RuleFor(x => x.ZonaId)
                .GreaterThan(0).WithMessage("La postazione deve appartenere a una zona valida.");
        }
    }
}
