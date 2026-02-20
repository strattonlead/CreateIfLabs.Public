using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper
{
    /// <summary>
    /// Internal mapping engine that builds and caches mapping plans.
    /// </summary>
    internal class MappingEngine
    {
        private readonly Dictionary<(Type, Type), TypeMap> _typeMaps;
        private readonly ConcurrentDictionary<(Type, Type), MappingPlan> _planCache
            = new ConcurrentDictionary<(Type, Type), MappingPlan>();

        internal MappingEngine(IEnumerable<TypeMap> typeMaps)
        {
            _typeMaps = new Dictionary<(Type, Type), TypeMap>();
            foreach (var tm in typeMaps)
            {
                _typeMaps[(tm.SourceType, tm.DestinationType)] = tm;
            }
        }

        internal bool HasMap(Type sourceType, Type destinationType)
        {
            return _typeMaps.ContainsKey((sourceType, destinationType));
        }

        internal object Map(object source, Type sourceType, Type destinationType,
            Func<Type, object> serviceCtor)
        {
            if (source == null)
            {
                return GetDefault(destinationType);
            }

            // Check if collection mapping
            if (TryMapCollection(source, sourceType, destinationType, serviceCtor, out var collectionResult))
            {
                return collectionResult;
            }

            // Direct assignment
            if (destinationType.IsAssignableFrom(sourceType))
            {
                return source;
            }

            var plan = GetOrBuildPlan(sourceType, destinationType);
            var destination = CreateInstance(destinationType);
            ExecutePlan(plan, source, destination, serviceCtor);
            return destination;
        }

        internal object MapToExisting(object source, object destination, Type sourceType, Type destinationType,
            Func<Type, object> serviceCtor)
        {
            if (source == null)
            {
                // When mapping into existing instance with null source, do not modify destination
                return destination;
            }

            var plan = GetOrBuildPlan(sourceType, destinationType);
            ExecutePlan(plan, source, destination, serviceCtor);
            return destination;
        }

        private bool TryMapCollection(object source, Type sourceType, Type destinationType,
            Func<Type, object> serviceCtor, out object result)
        {
            result = null;

            // Check if source is IEnumerable and destination is a collection type
            Type destElementType = GetCollectionElementType(destinationType);
            Type srcElementType = GetEnumerableElementType(sourceType);

            if (destElementType == null || srcElementType == null)
                return false;

            if (!(source is IEnumerable sourceEnumerable))
                return false;

            // Create a List<destElementType>
            var listType = typeof(List<>).MakeGenericType(destElementType);
            var list = (IList)Activator.CreateInstance(listType);

            foreach (var item in sourceEnumerable)
            {
                if (item == null)
                {
                    list.Add(GetDefault(destElementType));
                }
                else if (destElementType.IsAssignableFrom(srcElementType))
                {
                    list.Add(item);
                }
                else
                {
                    var mapped = Map(item, srcElementType, destElementType, serviceCtor);
                    list.Add(mapped);
                }
            }

            // If destination is array, convert
            if (destinationType.IsArray)
            {
                var array = Array.CreateInstance(destElementType, list.Count);
                list.CopyTo(array, 0);
                result = array;
            }
            else
            {
                result = list;
            }
            return true;
        }

        private Type GetCollectionElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            // Check interfaces
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private Type GetEnumerableElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private MappingPlan GetOrBuildPlan(Type sourceType, Type destinationType)
        {
            return _planCache.GetOrAdd((sourceType, destinationType), key =>
                BuildPlan(key.Item1, key.Item2));
        }

        private MappingPlan BuildPlan(Type sourceType, Type destinationType)
        {
            var plan = new MappingPlan();

            _typeMaps.TryGetValue((sourceType, destinationType), out var typeMap);

            var destProperties = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var destProp in destProperties)
            {
                // Check for explicit configuration
                MemberMap memberMap = null;
                if (typeMap != null)
                {
                    memberMap = typeMap.MemberMaps.Find(m => m.DestinationMemberName == destProp.Name);
                }

                if (memberMap != null)
                {
                    if (memberMap.Ignored)
                    {
                        // Skip this property entirely
                        continue;
                    }

                    if (memberMap.CustomResolver != null)
                    {
                        var destPropCopy = destProp;
                        var resolver = memberMap.CustomResolver;
                        plan.PropertyActions.Add((src, dest) =>
                        {
                            var value = resolver(src, dest);
                            var converted = ConvertValue(value, destPropCopy.PropertyType);
                            destPropCopy.SetValue(dest, converted);
                        });
                        continue;
                    }
                }

                // Convention-based: match by name (case-insensitive)
                if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
                {
                    var destPropCopy = destProp;
                    var sourcePropCopy = sourceProp;

                    plan.PropertyActions.Add((src, dest) =>
                    {
                        var srcVal = sourcePropCopy.GetValue(src);
                        if (srcVal == null)
                        {
                            destPropCopy.SetValue(dest, GetDefault(destPropCopy.PropertyType));
                        }
                        else if (destPropCopy.PropertyType.IsAssignableFrom(sourcePropCopy.PropertyType))
                        {
                            destPropCopy.SetValue(dest, srcVal);
                        }
                        else if (HasMap(sourcePropCopy.PropertyType, destPropCopy.PropertyType))
                        {
                            // Nested mapping
                            var mapped = Map(srcVal, sourcePropCopy.PropertyType, destPropCopy.PropertyType, Activator.CreateInstance);
                            destPropCopy.SetValue(dest, mapped);
                        }
                        else if (IsCollectionMapping(sourcePropCopy.PropertyType, destPropCopy.PropertyType))
                        {
                            var mapped = Map(srcVal, sourcePropCopy.PropertyType, destPropCopy.PropertyType, Activator.CreateInstance);
                            destPropCopy.SetValue(dest, mapped);
                        }
                        else
                        {
                            // Try direct assignment with conversion
                            try
                            {
                                var converted = ConvertValue(srcVal, destPropCopy.PropertyType);
                                destPropCopy.SetValue(dest, converted);
                            }
                            catch
                            {
                                // Leave at default
                            }
                        }
                    });
                }
                // else: no matching source, leave at default
            }

            // AfterMap actions from TypeMap
            if (typeMap != null)
            {
                plan.AfterMapActions.AddRange(typeMap.AfterMapActions);
                plan.AfterMapActionTypes.AddRange(typeMap.AfterMapActionTypes);
            }

            return plan;
        }

        private bool IsCollectionMapping(Type sourceType, Type destType)
        {
            return GetEnumerableElementType(sourceType) != null && GetCollectionElementType(destType) != null;
        }

        private void ExecutePlan(MappingPlan plan, object source, object destination,
            Func<Type, object> serviceCtor)
        {
            foreach (var action in plan.PropertyActions)
            {
                action(source, destination);
            }

            var context = new ResolutionContext(new Dictionary<string, object>(), serviceCtor);

            foreach (var afterMap in plan.AfterMapActions)
            {
                afterMap(source, destination, context);
            }

            foreach (var actionType in plan.AfterMapActionTypes)
            {
                object actionInstance;
                try
                {
                    actionInstance = serviceCtor(actionType);
                }
                catch
                {
                    actionInstance = Activator.CreateInstance(actionType);
                }

                // Find the Process method via the IMappingAction interface
                var interfaces = actionType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMappingAction<,>))
                    .ToArray();

                if (interfaces.Length > 0)
                {
                    var processMethod = interfaces[0].GetMethod("Process");
                    processMethod.Invoke(actionInstance, new[] { source, destination, context });
                }
            }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return GetDefault(targetType);

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            // Handle nullable
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                return Convert.ChangeType(value, underlyingType);
            }

            return Convert.ChangeType(value, targetType);
        }

        private static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                // Check for Nullable<T>
                if (Nullable.GetUnderlyingType(type) != null)
                    return null;
                return Activator.CreateInstance(type);
            }
            return null;
        }

        private static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }
    }

    /// <summary>
    /// Cached mapping plan for a (source, dest) type pair.
    /// </summary>
    internal class MappingPlan
    {
        public List<Action<object, object>> PropertyActions { get; } = new List<Action<object, object>>();
        public List<Action<object, object, ResolutionContext>> AfterMapActions { get; }
            = new List<Action<object, object, ResolutionContext>>();
        public List<Type> AfterMapActionTypes { get; } = new List<Type>();
    }
}
