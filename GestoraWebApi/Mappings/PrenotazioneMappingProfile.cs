using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Services.Prenotazioni.DTOs;

namespace GestoraWebApi.Mappings
{
    public class PrenotazioneMappingProfile : Profile
    {
        public PrenotazioneMappingProfile()
        {
            CreateMap<PrenotazionePostazione, PostazioneAssegnataDTO>()
                .ForMember(dest => dest.Numero,   opt => opt.MapFrom(src => src.Postazione.Numero))
                .ForMember(dest => dest.NomeZona, opt => opt.MapFrom(src => src.Postazione.Zona != null ? src.Postazione.Zona.Nome : null))
                // NEW-001: ZonaId era dichiarato nel DTO ma non mappato, quindi usciva sempre 0 -
                // stesso schema di REV-001 (NumeroPosti rimasto a 0). Serve al frontend per
                // precompilare la zona nel modal di modifica: con 0 la select non trovava alcuna
                // opzione corrispondente e restava senza selezione.
                .ForMember(dest => dest.ZonaId,   opt => opt.MapFrom(src => src.Postazione.ZonaId));

            CreateMap<Prenotazione, PrenotazioneDTO>()
                .ForMember(dest => dest.NomeUtente, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : null))
                .ForMember(dest => dest.OraInizio,  opt => opt.MapFrom(src => src.FasciaOraria != null ? src.FasciaOraria.OrarioInizio.ToString("HH:mm") : null))
                .ForMember(dest => dest.OraFine,    opt => opt.MapFrom(src => src.FasciaOraria != null ? src.FasciaOraria.OrarioFine.ToString("HH:mm") : null))
                .ForMember(dest => dest.Postazioni, opt => opt.MapFrom(src => src.PrenotazioniPostazioni));

            CreateMap<PrenotazioneCreateDTO, Prenotazione>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
