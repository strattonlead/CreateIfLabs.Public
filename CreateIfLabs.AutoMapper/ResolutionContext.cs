using System;
using System.Collections.Generic;

namespace AutoMapper
{
    /// <summary>
    /// Context passed to mapping actions during resolution.
    /// </summary>
    public class ResolutionContext
    {
        public IDictionary<string, object> Items { get; }
        public Func<Type, object> ServiceCtor { get; }

        public ResolutionContext(IDictionary<string, object> items, Func<Type, object> serviceCtor)
        {
            Items = items ?? new Dictionary<string, object>();
            ServiceCtor = serviceCtor ?? Activator.CreateInstance;
        }

        public ResolutionContext() : this(new Dictionary<string, object>(), Activator.CreateInstance)
        {
        }
    }
}
