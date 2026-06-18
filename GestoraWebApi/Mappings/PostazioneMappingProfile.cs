using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Services.Postazioni.DTOs;

namespace GestoraWebApi.Mappings
{
    public class PostazioneMappingProfile : Profile
    {
        public PostazioneMappingProfile()
        {
            CreateMap<PostazioneDTO, Postazione>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => src.Numero))
                .ForMember(dest => dest.CapienzaMassima, opt => opt.MapFrom(src => src.CapienzaMassima))
                .ForMember(dest => dest.ZonaId, opt => opt.MapFrom(src => src.ZonaId))
                .ForMember(dest => dest.Attiva, opt => opt.MapFrom(src => src.Attiva));


            CreateMap<Postazione, PostazioneDTO>()
                .ForMember(dest => dest.Numero, opt => opt.MapFrom(src => src.Numero))
                .ForMember(dest => dest.CapienzaMassima, opt => opt.MapFrom(src => src.CapienzaMassima))
                .ForMember(dest => dest.ZonaId, opt => opt.MapFrom(src => src.ZonaId))
                .ForMember(dest => dest.Attiva, opt => opt.MapFrom(src => src.Attiva))
                .ForMember(dest => dest.PrenotazioneId, opt => opt.MapFrom(src => src.PrenotazioniPostazioni.Select(pp => pp.PrenotazioneId)));//mappa direttamente la lista delle PrenotazioneId

            // Mapping per update: mappa solo i campi consentiti
            CreateMap<PostazioneUpdateDTO, Postazione>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
