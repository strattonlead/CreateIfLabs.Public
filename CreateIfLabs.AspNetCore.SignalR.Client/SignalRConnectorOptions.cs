using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;

namespace CreateIfLabs.AspNetCore.SignalR.Client
{
    public sealed class SignalRConnectorOptions
    {
        /// <summary>Hub-URL (z. B. https://example.com/hub)</summary>
        public string Url { get; set; }

        /// <summary>Optionaler API-Key-Header (wird als X-API-KEY gesetzt, wenn vorhanden)</summary>
        public string ApiKey { get; set; }

        /// <summary>Name des API-Key-Headers (Default: X-API-KEY)</summary>
        public string ApiKeyHeaderName { get; set; } = "X-API-KEY";

        /// <summary>Zusätzliche Header</summary>
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Auto-Reconnect aktivieren (Default: true)</summary>
        public bool AutomaticReconnect { get; set; } = true;

        /// <summary>Optional: Access-Token-Factory für Bearer-Auth</summary>
        public Func<IServiceProvider, System.Threading.Tasks.Task<string>> AccessTokenFactory { get; set; }

        /// <summary>Feintuning der HttpConnectionOptions</summary>
        public Action<IServiceProvider, HttpConnectionOptions> ConfigureHttpOptions { get; set; }

        /// <summary>Feintuning des HubConnectionBuilder</summary>
        public Action<IServiceProvider, IHubConnectionBuilder> ConfigureBuilder { get; set; }
    }
}
