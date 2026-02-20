using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace CreateIfLabs.AutoMapper.Tests
{
    /// <summary>
    /// Drives real AutoMapper (loaded at runtime) entirely via reflection.
    /// Uses fully-typed wrappers to avoid CS1977 dynamic-lambda issues.
    /// </summary>
    public static class RealAutoMapperFactory
    {
        private static Assembly _realAsm;

        private static Assembly RealAsm
        {
            get
            {
                if (_realAsm == null)
                {
                    // Load from file to avoid any name confusion
                    var dir = AppDomain.CurrentDomain.BaseDirectory;
                    var path = System.IO.Path.Combine(dir, "AutoMapper.dll");
                    _realAsm = Assembly.LoadFrom(path);
                }
                return _realAsm;
            }
        }

        private static readonly Lazy<Type> T_MapperConfiguration =
            new Lazy<Type>(() => RealAsm.GetType("AutoMapper.MapperConfiguration"));
        private static readonly Lazy<Type> T_IMapperConfigExpr =
            new Lazy<Type>(() => RealAsm.GetType("AutoMapper.IMapperConfigurationExpression"));

        /// <summary>
        /// Creates a real AutoMapper IMapper.
        /// </summary>
        public static RealMapperWrapper CreateMapper(Action<RealConfigExpr> configAction)
        {
            var configExprType = T_IMapperConfigExpr.Value;
            var mapperConfigType = T_MapperConfiguration.Value;

            // Create Action<IMapperConfigurationExpression> via dynamic method
            var actionType = typeof(Action<>).MakeGenericType(configExprType);

            // We need to build a delegate of type Action<RealAutoMapper.IMapperConfigurationExpression>
            // that when called wraps the arg in RealConfigExpr and passes to our configAction
            var bridgeHolder = new ConfigBridgeHolder { ConfigAction = configAction };

            // Use a DynamicMethod or MethodInfo approach
            var invokeMethod = typeof(ConfigBridgeHolder).GetMethod("Invoke");
            var del = Delegate.CreateDelegate(actionType, bridgeHolder, invokeMethod);

            // Find a constructor whose first parameter is Action<IMapperConfigurationExpression>
            // AutoMapper 16 ctors also require an ILoggerFactory – pass null for extra params.
            var ctors = mapperConfigType.GetConstructors();
            ConstructorInfo matchingCtor = null;
            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length >= 1 && parameters[0].ParameterType == actionType)
                {
                    matchingCtor = ctor;
                    break;
                }
            }

            if (matchingCtor == null)
            {
                var ctorInfo = string.Join("; ", ctors.Select(c =>
                    $"({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.FullName))})"));
                throw new InvalidOperationException(
                    $"Cannot find MapperConfiguration ctor taking Action<IMapperConfigurationExpression>. " +
                    $"Available ctors: {ctorInfo}. " +
                    $"Looking for param type: {actionType.FullName}");
            }

            // Build args: first param is the delegate, remaining params get defaults
            var ctorParams = matchingCtor.GetParameters();
            var args = new object[ctorParams.Length];
            args[0] = del;
            for (int i = 1; i < ctorParams.Length; i++)
            {
                // AutoMapper 16 needs ILoggerFactory – provide a real one
                if (typeof(ILoggerFactory).IsAssignableFrom(ctorParams[i].ParameterType))
                    args[i] = LoggerFactory.Create(_ => { });
                else
                    args[i] = null;
            }

            var config = matchingCtor.Invoke(args);
            var createMapperMethod = config.GetType().GetMethod("CreateMapper", Type.EmptyTypes);
            var mapper = createMapperMethod.Invoke(config, null);
            return new RealMapperWrapper(mapper);
        }
    }

    /// <summary>
    /// Bridge that receives a real AutoMapper IMapperConfigurationExpression and wraps
    /// it in a RealConfigExpr before calling the user's config action.
    /// </summary>
    public class ConfigBridgeHolder
    {
        public Action<RealConfigExpr> ConfigAction;
        public void Invoke(object configExpr) => ConfigAction(new RealConfigExpr(configExpr));
    }

    /// <summary>
    /// Typed wrapper around the real AutoMapper IMapperConfigurationExpression.
    /// </summary>
    public class RealConfigExpr
    {
        private readonly object _inner;
        public RealConfigExpr(object inner) => _inner = inner;

        public RealMappingExpr<TSource, TDest> CreateMap<TSource, TDest>()
        {
            var method = FindGenericMethod(_inner.GetType(), "CreateMap", 2, 0);
            var generic = method.MakeGenericMethod(typeof(TSource), typeof(TDest));
            var result = generic.Invoke(_inner, null);
            return new RealMappingExpr<TSource, TDest>(result);
        }

        internal static MethodInfo FindGenericMethod(Type type, string name, int genericArgs, int paramCount)
        {
            foreach (var t in new[] { type }.Concat(type.GetInterfaces()))
            {
                var m = t.GetMethods().FirstOrDefault(mi =>
                    mi.Name == name && mi.IsGenericMethod &&
                    mi.GetGenericArguments().Length == genericArgs &&
                    mi.GetParameters().Length == paramCount);
                if (m != null) return m;
            }
            throw new InvalidOperationException($"Cannot find {name}<{genericArgs}>({paramCount} params)");
        }
    }

    /// <summary>
    /// Typed wrapper around the real AutoMapper IMappingExpression.
    /// </summary>
    public class RealMappingExpr<TSource, TDest>
    {
        private readonly object _inner;
        public RealMappingExpr(object inner) => _inner = inner;

        public RealMappingExpr<TSource, TDest> ForMember<TMember>(
            Expression<Func<TDest, TMember>> destSelector,
            Action<RealMemberConfigExpr<TSource>> optionsAction)
        {
            MethodInfo forMemberGeneric = null;
            foreach (var iface in _inner.GetType().GetInterfaces())
            {
                forMemberGeneric = iface.GetMethods().FirstOrDefault(m =>
                    m.Name == "ForMember" && m.IsGenericMethod && m.GetParameters().Length == 2);
                if (forMemberGeneric != null) break;
            }
            if (forMemberGeneric == null)
                throw new InvalidOperationException("Cannot find ForMember on real IMappingExpression");

            var fm = forMemberGeneric.MakeGenericMethod(typeof(TMember));
            var actionParamType = fm.GetParameters()[1].ParameterType;

            var bridge = new MemberConfigBridgeHolder<TSource> { OptionsAction = optionsAction };
            var bridgeMethod = typeof(MemberConfigBridgeHolder<TSource>).GetMethod("Invoke");
            var bridgeDel = Delegate.CreateDelegate(actionParamType, bridge, bridgeMethod);

            fm.Invoke(_inner, new object[] { destSelector, bridgeDel });
            return this;
        }

        public RealMappingExpr<TSource, TDest> AfterMap(Action<TSource, TDest> afterAction)
        {
            MethodInfo am = null;
            foreach (var iface in _inner.GetType().GetInterfaces())
            {
                am = iface.GetMethods().FirstOrDefault(m =>
                    m.Name == "AfterMap" && !m.IsGenericMethod && m.GetParameters().Length == 1);
                if (am != null) break;
            }
            if (am == null)
                throw new InvalidOperationException("Cannot find AfterMap on real IMappingExpression");

            am.Invoke(_inner, new object[] { afterAction });
            return this;
        }
    }

    public class MemberConfigBridgeHolder<TSource>
    {
        public Action<RealMemberConfigExpr<TSource>> OptionsAction;
        public void Invoke(object mce) => OptionsAction(new RealMemberConfigExpr<TSource>(mce));
    }

    /// <summary>
    /// Typed wrapper around the real AutoMapper IMemberConfigurationExpression.
    /// </summary>
    public class RealMemberConfigExpr<TSource>
    {
        private readonly object _inner;
        public RealMemberConfigExpr(object inner) => _inner = inner;

        public void MapFrom<TResult>(Expression<Func<TSource, TResult>> sourceSelector)
        {
            // AutoMapper 16 has multiple MapFrom overloads. We need the one taking:
            //   Expression<Func<TSource, TResult>>
            // which is a Func with 2 generic args (1 input + 1 return).
            MethodInfo mf = null;
            foreach (var iface in _inner.GetType().GetInterfaces())
            {
                foreach (var m in iface.GetMethods())
                {
                    if (m.Name != "MapFrom" || !m.IsGenericMethod) continue;
                    if (m.GetGenericArguments().Length != 1) continue;
                    var p = m.GetParameters();
                    if (p.Length != 1) continue;
                    var pt = p[0].ParameterType;
                    if (!pt.IsGenericType) continue;
                    // Must be Expression<Func<...>>
                    var def = pt.GetGenericTypeDefinition();
                    if (def != typeof(Expression<>)) continue;
                    var funcType = pt.GetGenericArguments()[0];
                    if (!funcType.IsGenericType) continue;
                    // Func<TSource, TResult> has 2 generic args
                    if (funcType.GetGenericArguments().Length == 2)
                    {
                        mf = m;
                        break;
                    }
                }
                if (mf != null) break;
            }
            if (mf == null)
                throw new InvalidOperationException("Cannot find MapFrom<TResult>(Expression<Func<TSource,TResult>>) on real IMemberConfigExpr");

            var generic = mf.MakeGenericMethod(typeof(TResult));
            generic.Invoke(_inner, new object[] { sourceSelector });
        }

        public void Ignore()
        {
            MethodInfo ig = null;
            foreach (var iface in _inner.GetType().GetInterfaces())
            {
                ig = iface.GetMethods().FirstOrDefault(m =>
                    m.Name == "Ignore" && m.GetParameters().Length == 0);
                if (ig != null) break;
            }
            if (ig == null)
                throw new InvalidOperationException("Cannot find Ignore on real IMemberConfigExpr");

            ig.Invoke(_inner, null);
        }
    }

    /// <summary>
    /// Typed wrapper around the real AutoMapper IMapper.
    /// </summary>
    public class RealMapperWrapper
    {
        private readonly object _inner;
        public RealMapperWrapper(object inner) => _inner = inner;

        public TDest Map<TSource, TDest>(TSource source)
        {
            var method = FindMapMethod(2, 1);
            var generic = method.MakeGenericMethod(typeof(TSource), typeof(TDest));
            return (TDest)generic.Invoke(_inner, new object[] { source });
        }

        public TDest MapToExisting<TSource, TDest>(TSource source, TDest destination)
        {
            // AutoMapper 16 has both Map<S,D>(S, D) and Map<S,D>(S, Action<IMappingOperationOptions>)
            // We need to find the one where the second parameter is actually TDest (a generic type param).
            MethodInfo method = null;
            foreach (var iface in _inner.GetType().GetInterfaces())
            {
                foreach (var mi in iface.GetMethods())
                {
                    if (mi.Name != "Map" || !mi.IsGenericMethod) continue;
                    if (mi.GetGenericArguments().Length != 2) continue;
                    var p = mi.GetParameters();
                    if (p.Length != 2) continue;
                    // The second param should be a generic type parameter (TDest), not Action<...>
                    if (p[1].ParameterType.IsGenericParameter)
                    {
                        method = mi;
                        break;
                    }
                }
                if (method != null) break;
            }
            if (method == null)
                throw new InvalidOperationException("Cannot find Map<S,D>(S, D) on real IMapper");

            var generic = method.MakeGenericMethod(typeof(TSource), typeof(TDest));
            return (TDest)generic.Invoke(_inner, new object[] { source, destination });
        }

        private MethodInfo FindMapMethod(int genericArgs, int paramCount)
        {
            foreach (var iface in _inner.GetType().GetInterfaces())
            {
                var m = iface.GetMethods().FirstOrDefault(mi =>
                    mi.Name == "Map" && mi.IsGenericMethod &&
                    mi.GetGenericArguments().Length == genericArgs &&
                    mi.GetParameters().Length == paramCount);
                if (m != null) return m;
            }
            throw new InvalidOperationException($"Cannot find Map<{genericArgs}>({paramCount} params)");
        }
    }

    /// <summary>
    /// Structural deep comparison utility.
    /// </summary>
    public static class DeepComparer
    {
        public static List<string> Compare(object expected, object actual, string path = "root")
        {
            var diffs = new List<string>();
            if (expected == null && actual == null) return diffs;
            if (expected == null) { diffs.Add($"{path}: expected null, got '{actual}'"); return diffs; }
            if (actual == null) { diffs.Add($"{path}: expected '{expected}', got null"); return diffs; }

            var type = expected.GetType();
            if (type != actual.GetType())
            {
                diffs.Add($"{path}: type mismatch – {type.Name} vs {actual.GetType().Name}");
                return diffs;
            }

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(Guid) || type.IsEnum)
            {
                if (!expected.Equals(actual))
                    diffs.Add($"{path}: '{expected}' vs '{actual}'");
                return diffs;
            }

            if (Nullable.GetUnderlyingType(type) != null)
            {
                if (!expected.Equals(actual))
                    diffs.Add($"{path}: '{expected}' vs '{actual}'");
                return diffs;
            }

            if (expected is IEnumerable ee && actual is IEnumerable ae && (type.IsGenericType || type.IsArray))
            {
                var el = ee.Cast<object>().ToList();
                var al = ae.Cast<object>().ToList();
                if (el.Count != al.Count) { diffs.Add($"{path}: count {el.Count} vs {al.Count}"); return diffs; }
                for (int i = 0; i < el.Count; i++)
                    diffs.AddRange(Compare(el[i], al[i], $"{path}[{i}]"));
                return diffs;
            }

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name))
            {
                diffs.AddRange(Compare(prop.GetValue(expected), prop.GetValue(actual),
                    $"{path}.{prop.Name}"));
            }
            return diffs;
        }
    }
}
