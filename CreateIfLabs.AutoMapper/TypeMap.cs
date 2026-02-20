using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMapper
{
    /// <summary>
    /// Internal representation of a configured type mapping.
    /// </summary>
    internal class TypeMap
    {
        public Type SourceType { get; }
        public Type DestinationType { get; }

        internal List<MemberMap> MemberMaps { get; } = new List<MemberMap>();
        internal List<Action<object, object, ResolutionContext>> AfterMapActions { get; } = new List<Action<object, object, ResolutionContext>>();
        internal List<Type> AfterMapActionTypes { get; } = new List<Type>();

        public TypeMap(Type sourceType, Type destinationType)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
        }
    }

    /// <summary>
    /// Represents the configuration for mapping a single destination member.
    /// </summary>
    internal class MemberMap
    {
        public string DestinationMemberName { get; set; }
        public bool Ignored { get; set; }
        public Func<object, object, object> CustomResolver { get; set; }
        public string SourceMemberName { get; set; }
    }

    /// <summary>
    /// Implementation of IMappingExpression for non-generic use.
    /// </summary>
    internal class MappingExpression : IMappingExpression
    {
        private readonly TypeMap _typeMap;
        private readonly MapperConfigurationExpression _configExpression;

        internal MappingExpression(TypeMap typeMap, MapperConfigurationExpression configExpression)
        {
            _typeMap = typeMap;
            _configExpression = configExpression;
        }

        public IMappingExpression ReverseMap()
        {
            return _configExpression.CreateMap(_typeMap.DestinationType, _typeMap.SourceType);
        }
    }

    /// <summary>
    /// Implementation of IMappingExpression that collects member configuration.
    /// </summary>
    internal class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
    {
        private readonly TypeMap _typeMap;
        private readonly MapperConfigurationExpression _configExpression;

        internal MappingExpression(TypeMap typeMap, MapperConfigurationExpression configExpression)
        {
            _typeMap = typeMap;
            _configExpression = configExpression;
        }

        public IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
        {
            var memberName = GetMemberName(destinationMember);
            var existing = _typeMap.MemberMaps.Find(m => m.DestinationMemberName == memberName);
            if (existing == null)
            {
                existing = new MemberMap { DestinationMemberName = memberName };
                _typeMap.MemberMaps.Add(existing);
            }
            var configExpr = new MemberConfigurationExpression<TSource, TDestination, TMember>(existing);
            memberOptions(configExpr);
            return this;
        }

        public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction)
        {
            _typeMap.AfterMapActions.Add((src, dest, ctx) => afterFunction((TSource)src, (TDestination)dest));
            return this;
        }

        public IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination>
        {
            _typeMap.AfterMapActionTypes.Add(typeof(TMappingAction));
            return this;
        }

        public IMappingExpression<TDestination, TSource> ReverseMap()
        {
            return _configExpression.CreateMap<TDestination, TSource>();
        }

        private static string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> expression)
        {
            if (expression.Body is MemberExpression memberExpr)
                return memberExpr.Member.Name;
            if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression innerMember)
                return innerMember.Member.Name;
            throw new ArgumentException("Expression must be a member access expression.", nameof(expression));
        }
    }

    /// <summary>
    /// Implementation of IMemberConfigurationExpression.
    /// </summary>
    internal class MemberConfigurationExpression<TSource, TDestination, TMember>
        : IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        private readonly MemberMap _memberMap;

        internal MemberConfigurationExpression(MemberMap memberMap)
        {
            _memberMap = memberMap;
        }

        public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
        {
            var compiled = sourceMember.Compile();
            _memberMap.CustomResolver = (src, dest) => compiled((TSource)src);
        }

        public void MapFrom<TResult>(Func<TSource, TDestination, TResult> resolver)
        {
            _memberMap.CustomResolver = (src, dest) => resolver((TSource)src, (TDestination)dest);
        }

        public void Ignore()
        {
            _memberMap.Ignored = true;
        }
    }
}
