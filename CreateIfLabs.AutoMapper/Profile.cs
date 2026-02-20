using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMapper
{
    /// <summary>
    /// Base class for organizing mapping configurations – drop-in for AutoMapper.Profile.
    /// </summary>
    public abstract class Profile
    {
        internal MapperConfigurationExpression ConfigurationExpression { get; set; }

        protected Profile()
        {
            // ConfigurationExpression is set externally before Configure() is called,
            // or lazily when used standalone.
            ConfigurationExpression = new MapperConfigurationExpression();
            Configure();
        }

        /// <summary>
        /// Override this method or use the constructor to call CreateMap.
        /// </summary>
        protected virtual void Configure() { }

        protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            return ConfigurationExpression.CreateMap<TSource, TDestination>();
        }

        internal List<TypeMap> GetTypeMaps()
        {
            return ConfigurationExpression.TypeMaps;
        }
    }
}
