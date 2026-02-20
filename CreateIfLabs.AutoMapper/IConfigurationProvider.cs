using System;

namespace AutoMapper
{
    /// <summary>
    /// Provides access to compiled mapping configuration.
    /// </summary>
    public interface IConfigurationProvider
    {
        IMapper CreateMapper();
        IMapper CreateMapper(Func<Type, object> serviceCtor);
    }
}
