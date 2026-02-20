using System;
using System.Collections.Generic;

namespace AutoMapper
{
    /// <summary>
    /// Root configuration object – drop-in for AutoMapper.MapperConfiguration.
    /// Implements IConfigurationProvider.
    /// </summary>
    public class MapperConfiguration : IConfigurationProvider
    {
        private readonly MappingEngine _engine;

        public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
        {
            var expression = new MapperConfigurationExpression();
            configure(expression);
            _engine = new MappingEngine(expression.TypeMaps);
        }

        public MapperConfiguration(Action<IMapperConfigurationExpression> configure, object loggerFactory)
            : this(configure)
        {
        }

        public IMapper CreateMapper()
        {
            return new Mapper(this, _engine, Activator.CreateInstance);
        }

        public IMapper CreateMapper(Func<Type, object> serviceCtor)
        {
            return new Mapper(this, _engine, serviceCtor ?? Activator.CreateInstance);
        }

        internal MappingEngine Engine => _engine;
    }
}
