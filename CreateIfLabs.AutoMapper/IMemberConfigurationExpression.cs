using System;
using System.Linq.Expressions;

namespace AutoMapper
{
    /// <summary>
    /// Configuration options for a single destination member.
    /// </summary>
    public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
        void MapFrom<TResult>(Func<TSource, TDestination, TResult> resolver);
        void Ignore();
    }
}
