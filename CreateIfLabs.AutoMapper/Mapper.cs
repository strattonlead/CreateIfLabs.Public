using System;

namespace AutoMapper
{
    /// <summary>
    /// Default IMapper implementation – drop-in for AutoMapper.Mapper.
    /// </summary>
    internal class Mapper : IMapper
    {
        private readonly MapperConfiguration _configuration;
        private readonly MappingEngine _engine;
        private readonly Func<Type, object> _serviceCtor;

        internal Mapper(MapperConfiguration configuration, MappingEngine engine, Func<Type, object> serviceCtor)
        {
            _configuration = configuration;
            _engine = engine;
            _serviceCtor = serviceCtor ?? Activator.CreateInstance;
        }

        public IConfigurationProvider ConfigurationProvider => _configuration;

        public TDestination Map<TDestination>(object source)
        {
            if (source == null)
            {
                return default;
            }
            var result = _engine.Map(source, source.GetType(), typeof(TDestination), _serviceCtor);
            return (TDestination)result;
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            var result = _engine.Map(source, typeof(TSource), typeof(TDestination), _serviceCtor);
            if (result == null)
                return default;
            return (TDestination)result;
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            var result = _engine.MapToExisting(source, destination, typeof(TSource), typeof(TDestination), _serviceCtor);
            if (result == null)
                return default;
            return (TDestination)result;
        }

        public object Map(object source, Type sourceType, Type destinationType)
        {
            return _engine.Map(source, sourceType, destinationType, _serviceCtor);
        }

        public object Map(object source, object destination, Type sourceType, Type destinationType)
        {
            return _engine.MapToExisting(source, destination, sourceType, destinationType, _serviceCtor);
        }
    }
}
