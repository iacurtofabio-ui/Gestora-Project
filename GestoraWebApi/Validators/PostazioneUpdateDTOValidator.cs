using FluentValidation;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Validators
{
    public class PostazioneUpdateDTOValidator : AbstractValidator<PostazioneUpdateDTO>
    {
        public PostazioneUpdateDTOValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("ID postazione non valido.");

            RuleFor(x => x.Numero)
                .GreaterThan(0).WithMessage("Numero postazione non valido.");

            RuleFor(x => x.CapienzaMassima)
                .GreaterThan(0).WithMessage("La capienza deve essere almeno 1 posto.");

            RuleFor(x => x.ZonaId)
                .GreaterThan(0).WithMessage("La postazione deve appartenere a una zona valida.");
        }
    }
}
