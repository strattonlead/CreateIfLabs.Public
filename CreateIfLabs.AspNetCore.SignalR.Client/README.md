# 🛰️ CreateIfLabs.AspNetCore.SignalR.Client

Type-safe, dependency injection–friendly SignalR client with support for multiple endpoints  
(using **Named Options** and **Keyed Services**, available in .NET 8).

---

## 🚀 Installation

Add the project or NuGet package to your application:

```bash
dotnet add package CreateIfLabs.AspNetCore.SignalR.Client
```

---

## ⚙️ Configuration & Registration

Register your client inside `Program.cs` (or `Startup.cs`).

### 🔹 Single Client (one hub)

```csharp
using CreateIfLabs.AspNetCore.SignalR.Client;

builder.Services.AddSignalRConnector<IMyHub>(options =>
{
    options
        .WithUrl("https://example.com/myhub")
        .WithApiKey("super-secret")
        .WithAutomaticReconnect();
});
```

This automatically registers a `SignalRClient<IMyHub>` in the DI container,  
which you can inject directly into your services or hosted workers.

```csharp
public class Worker
{
    private readonly SignalRClient<IMyHub> _hubClient;

    public Worker(SignalRClient<IMyHub> hubClient)
    {
        _hubClient = hubClient;
    }

    public async Task RunAsync()
    {
        await _hubClient.ConnectAsync();

        // Example: invoke a server-side method "Ping"
        await _hubClient.InvokeAsync<IMyHubServer>(x => x.Ping());

        await _hubClient.DisconnectAsync();
    }
}
```

---

### 🔹 Multiple Hubs / Endpoints (Named Options + Keyed Services, .NET 8)

To connect to multiple different endpoints:

```csharp
builder.Services.AddSignalRConnector<IMyHub>("hubA", options =>
{
    options
        .WithUrl("https://api.example.com/hubA")
        .WithApiKey("key-A")
        .WithAutomaticReconnect();
});

builder.Services.AddSignalRConnector<IMyHub>("hubB", options =>
{
    options
        .WithUrl("https://api.example.com/hubB")
        .WithApiKey("key-B");
});
```

Then inject the specific clients by key:

```csharp
using Microsoft.Extensions.DependencyInjection;

public class Worker
{
    private readonly SignalRClient<IMyHub> _hubA;
    private readonly SignalRClient<IMyHub> _hubB;

    public Worker(
        [FromKeyedServices("hubA")] SignalRClient<IMyHub> hubA,
        [FromKeyedServices("hubB")] SignalRClient<IMyHub> hubB)
    {
        _hubA = hubA;
        _hubB = hubB;
    }

    public async Task RunAsync()
    {
        await _hubA.ConnectAsync();
        await _hubB.ConnectAsync();

        await _hubA.InvokeAsync<IMyHubServer>(x => x.Notify("Hello Hub A!"));
        await _hubB.InvokeAsync<IMyHubServer>(x => x.Notify("Hello Hub B!"));
    }
}
```

Or resolve manually via `IServiceProvider`:

```csharp
var hubA = provider.GetRequiredKeyedService<SignalRClient<IMyHub>>("hubA");
var hubB = provider.GetRequiredKeyedService<SignalRClient<IMyHub>>("hubB");
```

---

## 🧩 Interface Structure

- `TInterface` → defines **client handler methods** that the server can call  
  (these are automatically registered via `RegisterInterfaceHandler`).

- `TServer` → defines **server-side methods** that the client can invoke  
  (used in calls like `InvokeAsync<TServer>`).

Example:

```csharp
public interface IMyHub
{
    // Called from the server
    Task DeviceConnected(string id);
}

public interface IMyHubServer
{
    // Called from the client
    Task Notify(string message);
    Task<int> Add(int a, int b);
}
```

Usage:
```csharp
await client.InvokeAsync<IMyHubServer>(x => x.Notify("Hello!"));
var sum = await client.InvokeAsync<IMyHubServer, int>(x => x.Add(2, 3));
```

---

## 🧠 Lifecycle

- Each `SignalRClient<T>` instance is a **singleton** managing its internal `HubConnection`.  
- Call `ConnectAsync()` once at startup or on-demand.  
- Call `DisconnectAsync()` during shutdown or when manually reconnecting.  

---

## 🧰 Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalRConnector<IMyHub>(o =>
{
    o.WithUrl("https://localhost:5001/hub")
     .WithApiKey("demo-key");
});

builder.Services.AddHostedService<Worker>();
var app = builder.Build();
await app.RunAsync();
```

```csharp
public class Worker : IHostedService
{
    private readonly SignalRClient<IMyHub> _client;

    public Worker(SignalRClient<IMyHub> client) => _client = client;

    public async Task StartAsync(CancellationToken ct)
    {
        await _client.ConnectAsync(ct);
        await _client.InvokeAsync<IMyHubServer>(x => x.Notify("Worker started!"));
    }

    public Task StopAsync(CancellationToken ct) => _client.DisconnectAsync(ct);
}
```

---

✅ **Done:**  
With this setup you can register and configure any number of SignalR clients,  
all fully type-safe and DI-friendly — clean, composable, and testable.
