using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CreateIfLabs.AspNetCore.SignalR.Client
{
    public static class SignalRClientDiRegistrar
    {
        public static IReadOnlyList<IDisposable> RegisterInterfaceHandler<TInterface>(this HubConnection connection, IServiceProvider rootProvider)
        {
            if (!typeof(TInterface).IsInterface)
                throw new InvalidOperationException($"{typeof(TInterface).Name} ist kein Interface.");

            var disposables = new List<IDisposable>();

            foreach (var m in typeof(TInterface).GetMethods())
            {
                var paramTypes = m.GetParameters().Select(p => p.ParameterType).ToArray();
                var returnType = m.ReturnType;

                var disp = connection.On(
                    m.Name,
                    paramTypes,
                    async (args, _state) =>
                    {
                        await using var scope = rootProvider.CreateAsyncScope();
                        var impl = scope.ServiceProvider.GetRequiredService<TInterface>();

                        object callResult;
                        try
                        {
                            callResult = m.Invoke(impl, args);
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException is not null)
                        {
                            throw ex.InnerException;
                        }

                        return await UnwrapAsyncReturn(callResult, returnType).ConfigureAwait(false);
                    },
                    state: null);

                disposables.Add(disp);
            }

            return disposables;
        }

        private static async Task<object> UnwrapAsyncReturn(object callResult, Type returnType)
        {
            if (returnType == typeof(void)) return null;

            if (typeof(Task).IsAssignableFrom(returnType))
            {
                var task = (Task)callResult!;
                await task.ConfigureAwait(false);
                if (returnType.IsGenericType)
                    return returnType.GetProperty("Result")!.GetValue(task);
                return null;
            }

            if (returnType.FullName!.StartsWith("System.Threading.Tasks.ValueTask"))
            {
                var asTask = returnType.GetMethod("AsTask")!;
                var t = (Task)asTask.Invoke(callResult!, null)!;
                await t.ConfigureAwait(false);
                if (t.GetType().IsGenericType)
                    return t.GetType().GetProperty("Result")!.GetValue(t);
                return null;
            }

            return callResult;
        }
    }
}
