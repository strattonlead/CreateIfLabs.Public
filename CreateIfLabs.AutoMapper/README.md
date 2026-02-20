# CreateIfLabs.AutoMapper

An open-source, lightweight, drop-in replacement for AutoMapper.

Swap your NuGet reference and keep your existing mapping code — no namespace changes, no API rewrites.

---

## Why?

AutoMapper 16+ requires a paid license. This library provides core mapping functionality as a **free, open-source alternative** for projects that rely on AutoMapper's most common features.

## Features

- ✅ Convention-based property mapping (case-insensitive name matching)
- ✅ `Profile` base class for organizing mappings
- ✅ `ForMember` + `MapFrom` (expression-based custom projections)
- ✅ `ForMember` + `Ignore`
- ✅ `AfterMap` — lambda and DI-resolved action classes (`IMappingAction<S,D>`)
- ✅ `ReverseMap`
- ✅ Nested object mapping (recursive)
- ✅ Collection mapping (`List<T>`, `IEnumerable<T>`, `T[]`, etc.)
- ✅ Null propagation (null source → null destination)
- ✅ Map into existing destination instances
- ✅ Full DI integration via `AddAutoMapper()` with assembly scanning
- ✅ `netstandard2.1` — compatible with .NET Core 3+, .NET 5+, .NET Framework 4.8

## Installation

Add a project reference or package reference to `CreateIfLabs.AutoMapper`. Then use the `AutoMapper` namespace as you normally would.

```xml
<PackageReference Include="CreateIfLabs.AutoMapper" Version="1.0.0" />
```

## Quick Start

### Inline Configuration

```csharp
using AutoMapper;

var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Source, Destination>();
});

IMapper mapper = config.CreateMapper();
var dest = mapper.Map<Source, Destination>(source);
```

### Using Profiles

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Total, opt => opt.MapFrom(s => s.Price * s.Quantity))
            .ForMember(d => d.InternalNotes, opt => opt.Ignore());

        CreateMap<OrderItem, OrderItemDto>();
    }
}
```

### Dependency Injection

```csharp
// Startup / Program.cs
services.AddAutoMapper(typeof(OrderProfile)); // scans assembly for all Profiles

// Or with inline config:
services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<Order, OrderDto>();
});
```

```csharp
// In your service
public class OrderService
{
    private readonly IMapper _mapper;
    public OrderService(IMapper mapper) => _mapper = mapper;

    public OrderDto GetOrder(Order order) => _mapper.Map<OrderDto>(order);
}
```

### AfterMap with DI

```csharp
public class EnrichOrderAction : IMappingAction<Order, OrderDto>
{
    private readonly IPricingService _pricing;
    public EnrichOrderAction(IPricingService pricing) => _pricing = pricing;

    public void Process(Order source, OrderDto destination, ResolutionContext context)
    {
        destination.FormattedTotal = _pricing.Format(source.Total);
    }
}

// In your profile:
CreateMap<Order, OrderDto>()
    .AfterMap<EnrichOrderAction>();
```

### Mapping into Existing Objects

```csharp
var existing = new OrderDto { Notes = "preserve me" };
mapper.Map(source, existing);
// existing.Notes is unchanged, mapped properties are updated
```

## DI Registration Details

| Registration | Lifetime |
|-------------|----------|
| `MapperConfiguration` | Singleton |
| `IConfigurationProvider` | Singleton |
| `IMapper` | Transient |

`AddAutoMapper` overloads:

| Signature | Description |
|-----------|-------------|
| `AddAutoMapper(Action<IMapperConfigurationExpression>)` | Manual configuration |
| `AddAutoMapper(params Assembly[])` | Assembly scanning |
| `AddAutoMapper(params Type[])` | Marker type scanning |
| `AddAutoMapper(Action, params Assembly[])` | Config + assembly scan |
| `AddAutoMapper(Action, params Type[])` | Config + marker types |

## Migrating from AutoMapper

1. Remove the `AutoMapper` NuGet package
2. Add `CreateIfLabs.AutoMapper`
3. Remove `AutoMapper.Extensions.Microsoft.DependencyInjection` (DI is built-in)
4. **Done** — no code changes needed

> [!NOTE]
> If you use advanced AutoMapper features not listed above, check the [Limitations](#limitations) section.

## Limitations

This library intentionally covers the most commonly used subset of AutoMapper. The following features are **not** supported:

| Feature | Status |
|---------|--------|
| `ProjectTo<T>()` (IQueryable) | ❌ |
| `ConstructUsing` / `ConvertUsing` | ❌ |
| Value / Type converters | ❌ |
| `BeforeMap` | ❌ |
| `Condition` / `PreCondition` | ❌ |
| Inheritance mapping (`IncludeBase`/`IncludeDerived`) | ❌ |
| Flattening (`Order.Customer.Name` → `CustomerName`) | ❌ |
| `MaxDepth` / `PreserveReferences` | ❌ |
| Expression tree compilation | ❌ (uses reflection + caching) |

## How It Works

The mapping engine uses **reflection with plan caching**:

1. On first `Map()` call for a type pair, a `MappingPlan` is built by matching source/destination properties
2. Explicit `ForMember` overrides are applied (custom resolvers, ignores)
3. The plan is cached in a `ConcurrentDictionary` — subsequent calls skip plan building
4. `AfterMap` actions run after all properties are mapped

## License

MIT — see [LICENSE](../LICENSE) for details.
