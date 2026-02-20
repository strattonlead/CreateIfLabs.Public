using System;
using System.Linq;
using System.Reflection;
using AutoMapper;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// DI extension methods for registering the AutoMapper-compatible library.
    /// Placed in Microsoft.Extensions.DependencyInjection namespace to match the real AutoMapper pattern.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AutoMapper-compatible services using a configuration action.
        /// </summary>
        public static IServiceCollection AddAutoMapper(this IServiceCollection services,
            Action<IMapperConfigurationExpression> configAction)
        {
            var config = new MapperConfiguration(configAction);
            services.AddSingleton(config);
            services.AddSingleton<IConfigurationProvider>(sp => sp.GetRequiredService<MapperConfiguration>());
            services.AddTransient<IMapper>(sp =>
            {
                var cfg = sp.GetRequiredService<MapperConfiguration>();
                return cfg.CreateMapper(type =>
                {
                    try { return sp.GetService(type) ?? Activator.CreateInstance(type); }
                    catch { return Activator.CreateInstance(type); }
                });
            });
            return services;
        }

        /// <summary>
        /// Adds AutoMapper-compatible services by scanning assemblies for Profile types.
        /// </summary>
        public static IServiceCollection AddAutoMapper(this IServiceCollection services,
            params Assembly[] assemblies)
        {
            return services.AddAutoMapper(cfg => cfg.AddMaps(assemblies));
        }

        /// <summary>
        /// Adds AutoMapper-compatible services by scanning assemblies of the given marker types.
        /// </summary>
        public static IServiceCollection AddAutoMapper(this IServiceCollection services,
            params Type[] profileAssemblyMarkerTypes)
        {
            var assemblies = profileAssemblyMarkerTypes
                .Select(t => t.Assembly)
                .Distinct()
                .ToArray();
            return services.AddAutoMapper(assemblies);
        }

        /// <summary>
        /// Adds AutoMapper-compatible services using a configuration action and assembly scanning.
        /// </summary>
        public static IServiceCollection AddAutoMapper(this IServiceCollection services,
            Action<IMapperConfigurationExpression> configAction,
            params Assembly[] assemblies)
        {
            return services.AddAutoMapper(cfg =>
            {
                configAction(cfg);
                cfg.AddMaps(assemblies);
            });
        }

        /// <summary>
        /// Adds AutoMapper-compatible services using a configuration action and marker types for assembly scanning.
        /// </summary>
        public static IServiceCollection AddAutoMapper(this IServiceCollection services,
            Action<IMapperConfigurationExpression> configAction,
            params Type[] profileAssemblyMarkerTypes)
        {
            var assemblies = profileAssemblyMarkerTypes
                .Select(t => t.Assembly)
                .Distinct()
                .ToArray();
            return services.AddAutoMapper(configAction, assemblies);
        }
    }
}
