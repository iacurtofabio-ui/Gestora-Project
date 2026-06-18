using FluentValidation;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Validators
{
    public class PostazioneUpdateDTOValidator : AbstractValidator<PostazioneUpdateDTO>
    {
        private static readonly int[] CapienzaConsentita = { 2, 4, 8 };

        public PostazioneUpdateDTOValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("ID postazione non valido.");

            RuleFor(x => x.Numero)
                .GreaterThan(0).WithMessage("Numero postazione non valido.");

            RuleFor(x => x.CapienzaMassima)
                .Must(c => CapienzaConsentita.Contains(c))
                .WithMessage($"Capienza consentita: {string.Join(", ", CapienzaConsentita)}.");

            RuleFor(x => x.ZonaId)
                .GreaterThan(0).WithMessage("La postazione deve appartenere a una zona valida.");
        }
    }
}
