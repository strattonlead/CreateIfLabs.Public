using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CreateIfLabs.AspNetCore.SignalR.Client
{
    public class SignalRClient<TInterface>
    {
        private readonly IServiceProvider _serviceProvider;
        private IReadOnlyList<IDisposable> _subscriptions;
        private HubConnection _connection;
        private readonly SignalRConnectorOptions _options;

        public event DeviceConnectedHandler OnDeviceConnected;
        public event DeviceConnectedHandlerAsync OnDeviceConnectedAsync;

        public SignalRClient(IServiceProvider serviceProvider, IOptionsMonitor<SignalRConnectorOptions> options, string optionsName)
        {
            _serviceProvider = serviceProvider;
            _options = options.Get(optionsName ?? Options.DefaultName);
        }

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            if (_connection is { State: HubConnectionState.Connected })
                return;

            if (_options is null || string.IsNullOrWhiteSpace(_options.Url))
                throw new InvalidOperationException("SignalRConnectorOptions.Url ist nicht gesetzt.");

            var builder = new HubConnectionBuilder()
                .WithUrl(_options.Url, http =>
                {
                    // API-Key (optional)
                    if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                    {
                        http.Headers[_options.ApiKeyHeaderName ?? "X-API-KEY"] = _options.ApiKey;
                    }

                    // Weitere Header
                    foreach (var kv in _options.Headers)
                    {
                        http.Headers[kv.Key] = kv.Value;
                    }

                    // Optional: Bearer / Access Token
                    if (_options.AccessTokenFactory is not null)
                    {
                        http.AccessTokenProvider = () => _options.AccessTokenFactory(_serviceProvider);
                    }

                    // Optional: feines Http-Tuning
                    _options.ConfigureHttpOptions?.Invoke(_serviceProvider, http);
                });

            if (_options.AutomaticReconnect)
                builder = builder.WithAutomaticReconnect();

            // Optional: Builder-Hooks (z. B. Protokoll, Logging)
            _options.ConfigureBuilder?.Invoke(_serviceProvider, builder);

            _connection = builder.Build();
            _subscriptions = _connection.RegisterInterfaceHandler<TInterface>(_serviceProvider);

            await _connection.StartAsync(ct).ConfigureAwait(false);
            await RaiseDeviceConnectedAsync().ConfigureAwait(false);
        }

        public async Task DisconnectAsync(CancellationToken ct = default)
        {
            if (_subscriptions != null)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
                _subscriptions = null;
            }

            await _connection.StopAsync(ct);
        }

        #region Helper

        public Task RaiseDeviceConnectedAsync()
        {
            OnDeviceConnected?.Invoke();
            var handlers = OnDeviceConnectedAsync;
            return handlers is null
                ? Task.CompletedTask
                : Task.WhenAll(handlers
                    .GetInvocationList()
                    .Cast<DeviceConnectedHandlerAsync>()
                    .Select(h => h()));
        }


        /// <summary>
        ///  Fire and Forget
        /// </summary>
        public Task SendAsync<TServer>(Expression<Action<TServer>> call, CancellationToken ct = default)
        {
            _ensureConnected();
            var (name, args) = _parse(call);
            return _connection.SendCoreAsync(name, args, ct);
        }

        /// <summary>
        /// Wartet bis der Server die Ausführung vollendet hat
        /// </summary>
        public Task InvokeAsync<TServer>(Expression<Func<TServer, Task>> call, CancellationToken ct = default)
        {
            _ensureConnected();
            var (name, args) = _parse(call);
            return _connection.InvokeCoreAsync(name, args, ct);
        }

        /// <summary>
        /// Wartet bis der Server die Ausführung vollendet hat + Ergebnis
        /// </summary>
        public Task<TResult> InvokeAsync<TServer, TResult>(Expression<Func<TServer, Task<TResult>>> call, CancellationToken ct = default)
        {
            _ensureConnected();
            var (name, args) = _parse(call);
            return _connection.InvokeCoreAsync<TResult>(name, args, ct);
        }

        /// <summary>
        /// Streaming vom Server
        /// </summary>
        public IAsyncEnumerable<TResult> StreamAsync<TServer, TResult>(Expression<Func<TServer, IAsyncEnumerable<TResult>>> call, CancellationToken ct = default)
        {
            _ensureConnected();
            var (name, args) = _parse(call);
            return _connection.StreamAsync<TResult>(name, args, ct);
        }

        private static (string name, object[] args) _parse<TServer>(Expression<Action<TServer>> expr)
        {
            if (expr.Body is not MethodCallExpression mce)
                throw new ArgumentException("Expression muss ein Methodenaufruf sein, z. B. x => x.Foo(a,b).", nameof(expr));

            var name = mce.Method.Name;
            var args = mce.Arguments.Select(_evalToObject).ToArray();
            return (name, args);
        }

        private static (string name, object[] args) _parse<TServer, TBody>(Expression<Func<TServer, TBody>> expr)
        {
            if (expr.Body is not MethodCallExpression mce)
                throw new ArgumentException("Expression muss ein Methodenaufruf sein, z. B. x => x.Foo(a,b).", nameof(expr));

            var name = mce.Method.Name;
            var args = mce.Arguments.Select(_evalToObject).Where(x => x is not CancellationToken).ToArray();
            return (name, args);
        }

        private static object _evalToObject(Expression element)
        {
            if (element is ConstantExpression)
            {
                return (element as ConstantExpression).Value;
            }

            var l = Expression.Lambda(Expression.Convert(element, element.Type));
            return l.Compile().DynamicInvoke();
        }

        private void _ensureConnected()
        {
            if (_connection is null)
            {
                throw new InvalidOperationException("HubConnection ist null.");
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                throw new InvalidOperationException("HubConnection ist nicht verbunden.");
            }
        }

        #endregion
    }

    public delegate void DeviceConnectedHandler();
    public delegate Task DeviceConnectedHandlerAsync();
}
