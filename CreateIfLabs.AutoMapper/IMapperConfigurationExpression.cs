using System;
using System.Reflection;

namespace AutoMapper
{
    /// <summary>
    /// Configuration expression for defining maps and adding profiles.
    /// </summary>
    public interface IMapperConfigurationExpression
    {
        IMappingExpression CreateMap(Type sourceType, Type destinationType);
        IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
        void AddProfile(Profile profile);
        void AddProfile<TProfile>() where TProfile : Profile, new();
        void AddProfile(Type profileType);
        void AddMaps(params Assembly[] assemblies);
    }
}
