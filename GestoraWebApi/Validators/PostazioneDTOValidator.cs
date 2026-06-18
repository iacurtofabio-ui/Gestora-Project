using FluentValidation;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Validators
{
    public class PostazioneDTOValidator : AbstractValidator<PostazioneDTO>
    {
        private static readonly int[] CapienzaConsentita = { 2, 4, 8 };

        public PostazioneDTOValidator()
        {
            RuleFor(x => x.Numero)
                .GreaterThan(0).WithMessage("Specificare un numero valido per la postazione.");

            RuleFor(x => x.CapienzaMassima)
                .Must(c => CapienzaConsentita.Contains(c))
                .WithMessage($"La capienza deve essere uno di: {string.Join(", ", CapienzaConsentita)}.");

            RuleFor(x => x.ZonaId)
                .GreaterThan(0).WithMessage("La postazione deve appartenere a una zona valida.");
        }
    }
}
