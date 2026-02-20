using System;
using System.Linq.Expressions;

namespace AutoMapper
{
    public interface IMappingExpression
    {
        IMappingExpression ReverseMap();
    }

    /// <summary>
    /// Fluent API for configuring a source-to-destination mapping.
    /// </summary>
    public interface IMappingExpression<TSource, TDestination>
    {
        IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

        IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction);
        IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination>;

        IMappingExpression<TDestination, TSource> ReverseMap();
    }
}
