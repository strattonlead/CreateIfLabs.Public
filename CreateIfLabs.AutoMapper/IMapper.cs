using System;
using System.Collections.Generic;

namespace AutoMapper
{
    /// <summary>
    /// Main mapping interface – drop-in replacement for AutoMapper.IMapper.
    /// </summary>
    public interface IMapper
    {
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        object Map(object source, Type sourceType, Type destinationType);
        object Map(object source, object destination, Type sourceType, Type destinationType);
        IConfigurationProvider ConfigurationProvider { get; }
    }
}
