using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMapper
{
    /// <summary>
    /// Implementation of IMapperConfigurationExpression that collects TypeMaps.
    /// </summary>
    internal class MapperConfigurationExpression : IMapperConfigurationExpression
    {
        internal List<TypeMap> TypeMaps { get; } = new List<TypeMap>();

        public IMappingExpression CreateMap(Type sourceType, Type destinationType)
        {
            var existing = TypeMaps.Find(t => t.SourceType == sourceType && t.DestinationType == destinationType);
            if (existing != null)
            {
                return new MappingExpression(existing, this);
            }

            var typeMap = new TypeMap(sourceType, destinationType);
            TypeMaps.Add(typeMap);
            return new MappingExpression(typeMap, this);
        }

        public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            var existing = TypeMaps.Find(t =>
                t.SourceType == typeof(TSource) && t.DestinationType == typeof(TDestination));

            if (existing != null)
            {
                return new MappingExpression<TSource, TDestination>(existing, this);
            }

            var typeMap = new TypeMap(typeof(TSource), typeof(TDestination));
            TypeMaps.Add(typeMap);
            return new MappingExpression<TSource, TDestination>(typeMap, this);
        }

        public void AddProfile(Profile profile)
        {
            // Merge the profile's type maps
            foreach (var tm in profile.GetTypeMaps())
            {
                var existing = TypeMaps.Find(t =>
                    t.SourceType == tm.SourceType && t.DestinationType == tm.DestinationType);
                if (existing == null)
                {
                    TypeMaps.Add(tm);
                }
                else
                {
                    // Merge member maps
                    foreach (var mm in tm.MemberMaps)
                    {
                        var existingMm = existing.MemberMaps.Find(m =>
                            m.DestinationMemberName == mm.DestinationMemberName);
                        if (existingMm == null)
                            existing.MemberMaps.Add(mm);
                    }
                    existing.AfterMapActions.AddRange(tm.AfterMapActions);
                    existing.AfterMapActionTypes.AddRange(tm.AfterMapActionTypes);
                }
            }
        }

        public void AddProfile<TProfile>() where TProfile : Profile, new()
        {
            AddProfile(new TProfile());
        }

        public void AddProfile(Type profileType)
        {
            if (!typeof(Profile).IsAssignableFrom(profileType))
                throw new ArgumentException($"Type {profileType.Name} does not inherit from Profile.", nameof(profileType));
            var profile = (Profile)Activator.CreateInstance(profileType);
            AddProfile(profile);
        }

        public void AddMaps(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

                foreach (var type in types)
                {
                    if (typeof(Profile).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                    {
                        AddProfile(type);
                    }
                }
            }
        }
    }
}
