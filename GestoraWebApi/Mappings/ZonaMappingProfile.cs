using AutoMapper;
using GestoraWebApi.Models;
using GestoraWebApi.Services.Zone.DTOs;

namespace GestoraWebApi.Mappings
{
    public class ZonaMappingProfile : Profile
    {

        public ZonaMappingProfile()
        {
            CreateMap<Zona, ZonaDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.Attiva, opt => opt.MapFrom(src => src.Attiva));

            CreateMap<ZonaDTO, Zona>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.Nome))
                .ForMember(dest => dest.Attiva, opt => opt.MapFrom(src => src.Attiva));
        }
    }
}
