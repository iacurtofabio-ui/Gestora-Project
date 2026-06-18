using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Services.FasciaOrarie.DTOs;

namespace GestoraWebApi.Mappings
{
    public class FasciaOrariaMappingProfile : Profile
    {
        public FasciaOrariaMappingProfile()
        {

            CreateMap<FasciaOrariaDTO, FasciaOraria>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrarioInizio, opt => opt.Ignore())
                .ForMember(dest => dest.OrarioFine, opt => opt.Ignore())
                .ForMember(dest => dest.GiornoSettimana, opt => opt.MapFrom(src => src.GiornoSettimana))
                .ForMember(dest => dest.MaxPrenotazioni, opt => opt.MapFrom(src => src.MaxPrenotazioni))
                .ForMember(dest => dest.Attiva, opt => opt.MapFrom(src => src.Attiva));

            CreateMap<FasciaOraria, FasciaOrariaDTO>()
                .ForMember(dest => dest.OrarioInizio, opt => opt.Ignore())
                .ForMember(dest => dest.OrarioFine, opt => opt.Ignore())
                .ForMember(dest => dest.GiornoSettimana, opt => opt.MapFrom(src => src.GiornoSettimana))
                .ForMember(dest => dest.MaxPrenotazioni, opt => opt.MapFrom(src => src.MaxPrenotazioni))
                .ForMember(dest => dest.Attiva, opt => opt.MapFrom(src => src.Attiva));
        }
    }
}
