using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace CreateIfLabs.AspNetCore.SignalR.Client
{
    public static class SignalRConnectorServiceCollectionExtensions
    {
        /// <summary>
        /// Ein einzelner (unnamed) Connector, auflösbar als unkeyed Service.
        /// </summary>
        public static IServiceCollection AddSignalRConnector<TInterface>(
            this IServiceCollection services,
            Action<SignalRConnectorOptionsBuilder> configure)
        {
            var name = Options.DefaultName;

            services.AddOptions<SignalRConnectorOptions>(name)
                .Configure<IServiceProvider>((opt, sp) =>
                {
                    var b = new SignalRConnectorOptionsBuilder();
                    configure?.Invoke(b);
                    var built = b.Build();

                    opt.Url = built.Url;
                    opt.ApiKey = built.ApiKey;
                    opt.AutomaticReconnect = built.AutomaticReconnect;
                    opt.ConfigureHttpOptions = built.ConfigureHttpOptions;
                    opt.ConfigureBuilder = built.ConfigureBuilder;
                    opt.AccessTokenFactory = built.AccessTokenFactory;

                    opt.Headers.Clear();
                    foreach (var kv in built.Headers)
                        opt.Headers[kv.Key] = kv.Value;
                });

            // Unkeyed Singleton – nur sinnvoll, wenn du genau EINEN Endpoint willst.
            services.AddSingleton<SignalRClient<TInterface>>(sp =>
            {
                var monitor = sp.GetRequiredService<IOptionsMonitor<SignalRConnectorOptions>>();
                return new SignalRClient<TInterface>(sp, monitor, name);
            });

            return services;
        }

        /// <summary>
        /// Mehrere Endpunkte via Named Options + Keyed Services.
        /// Auflösung dann mit [FromKeyedServices(name)] oder GetRequiredKeyedService.
        /// </summary>
        public static IServiceCollection AddSignalRConnector<TInterface>(
            this IServiceCollection services,
            string name,
            Action<SignalRConnectorOptionsBuilder> configure)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Options-/Service-Name darf nicht leer sein.", nameof(name));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            services.AddOptions<SignalRConnectorOptions>(name)
                .Configure<IServiceProvider>((opt, sp) =>
                {
                    var b = new SignalRConnectorOptionsBuilder();
                    configure(b);
                    var built = b.Build();

                    opt.Url = built.Url;
                    opt.ApiKey = built.ApiKey;
                    opt.AutomaticReconnect = built.AutomaticReconnect;
                    opt.ConfigureHttpOptions = built.ConfigureHttpOptions;
                    opt.ConfigureBuilder = built.ConfigureBuilder;
                    opt.AccessTokenFactory = built.AccessTokenFactory;

                    opt.Headers.Clear();
                    foreach (var kv in built.Headers)
                        opt.Headers[kv.Key] = kv.Value;
                });

            // Wichtig: keyed registrieren und den Key als optionsName durchreichen
            services.AddKeyedSingleton<SignalRClient<TInterface>>(name, (sp, key) =>
            {
                var monitor = sp.GetRequiredService<IOptionsMonitor<SignalRConnectorOptions>>();
                return new SignalRClient<TInterface>(sp, monitor, key?.ToString());
            });

            return services;
        }
    }
}
