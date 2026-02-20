namespace AutoMapper
{
    /// <summary>
    /// Marker interface for mapping actions resolved from DI.
    /// </summary>
    public interface IMappingAction<in TSource, in TDestination>
    {
        void Process(TSource source, TDestination destination, ResolutionContext context);
    }
}
