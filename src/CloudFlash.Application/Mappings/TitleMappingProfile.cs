using AutoMapper;
using CloudFlash.Application.DTOs;
using CloudFlash.Core.Entities;

namespace CloudFlash.Application.Mappings;

public class TitleMappingProfile : Profile
{
    public TitleMappingProfile()
    {
        CreateMap<Title, TitleSearchDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Genres, opt => opt.MapFrom(src => src.GenreIds));

        CreateMap<StreamingAvailability, StreamingAvailabilityDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<TitleSearchRequestDto, Queries.SearchTitlesQuery>();
    }
}
