using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;

namespace CreateIfLabs.AspNetCore.SignalR.Client
{
    public sealed class SignalRConnectorOptionsBuilder
    {
        private readonly SignalRConnectorOptions _options = new();

        internal SignalRConnectorOptions Build() => _options;

        public SignalRConnectorOptionsBuilder WithUrl(string url)
        {
            _options.Url = url;
            return this;
        }

        public SignalRConnectorOptionsBuilder WithApiKey(string apiKey)
        {
            _options.ApiKey = apiKey;
            return this;
        }

        public SignalRConnectorOptionsBuilder WithHeader(string name, string value)
        {
            _options.Headers[name] = value;
            return this;
        }

        public SignalRConnectorOptionsBuilder WithAutomaticReconnect(bool enabled = true)
        {
            _options.AutomaticReconnect = enabled;
            return this;
        }

        public SignalRConnectorOptionsBuilder WithAccessToken(Func<IServiceProvider, System.Threading.Tasks.Task<string>> factory)
        {
            _options.AccessTokenFactory = factory;
            return this;
        }

        public SignalRConnectorOptionsBuilder ConfigureHttp(Action<IServiceProvider, HttpConnectionOptions> configure)
        {
            _options.ConfigureHttpOptions = configure;
            return this;
        }

        public SignalRConnectorOptionsBuilder ConfigureBuilder(Action<IServiceProvider, IHubConnectionBuilder> configure)
        {
            _options.ConfigureBuilder = configure;
            return this;
        }
    }
}
