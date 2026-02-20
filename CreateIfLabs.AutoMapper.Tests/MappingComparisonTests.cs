using AutoMapper;
using CreateIfLabs.AutoMapper.Tests.Actions;
using CreateIfLabs.AutoMapper.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CreateIfLabs.AutoMapper.Tests
{
    public class MappingComparisonTests
    {
        // ────────────── Helpers ──────────────────────────────────────

        private IMapper CreateOurMapper(Action<IMapperConfigurationExpression> configure)
        {
            var config = new MapperConfiguration(configure);
            return config.CreateMapper();
        }

        private void AssertEquivalent(object expected, object actual, string scenario)
        {
            var diffs = DeepComparer.Compare(expected, actual, "result");
            if (diffs.Count > 0)
            {
                var message = $"[{scenario}] Mismatch between real AutoMapper and our library:\n"
                    + string.Join("\n", diffs.Select(d => "  • " + d));
                Assert.Fail(message);
            }
        }

        // ──── Test 1: Basic Property Mapping ─────────────────────────

        [Fact]
        public void BasicPropertyMapping_MatchesAutoMapper()
        {
            var source = new SimpleSource { Id = 42, Name = "Alice", Score = 9.5, IsActive = true };

            // Our library
            var ourMapper = CreateOurMapper(cfg => cfg.CreateMap<SimpleSource, SimpleDest>());
            var ourResult = ourMapper.Map<SimpleSource, SimpleDest>(source);

            // Real AutoMapper
            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
                cfg.CreateMap<SimpleSource, SimpleDest>());
            var realResult = realMapper.Map<SimpleSource, SimpleDest>(source);

            AssertEquivalent(realResult, ourResult, "BasicPropertyMapping");
        }

        // ──── Test 2: ForMember MapFrom ──────────────────────────────

        [Fact]
        public void ForMember_MapFrom_MatchesAutoMapper()
        {
            var source = new ForMemberSource
            {
                FirstName = "John",
                LastName = "Doe",
                Age = 30,
                Secret = "hidden"
            };

            // Our library
            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<ForMemberSource, ForMemberDest>()
                    .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName))
                    .ForMember(d => d.Secret, opt => opt.Ignore())
                    .ForMember(d => d.Extra, opt => opt.Ignore());
            });
            var ourResult = ourMapper.Map<ForMemberSource, ForMemberDest>(source);

            // Real AutoMapper
            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
            {
                cfg.CreateMap<ForMemberSource, ForMemberDest>()
                    .ForMember<string>(d => d.FullName,
                        opt => opt.MapFrom(s => s.FirstName + " " + s.LastName))
                    .ForMember<string>(d => d.Secret, opt => opt.Ignore())
                    .ForMember<string>(d => d.Extra, opt => opt.Ignore());
            });
            var realResult = realMapper.Map<ForMemberSource, ForMemberDest>(source);

            AssertEquivalent(realResult, ourResult, "ForMember_MapFrom");
            Assert.Equal("John Doe", ourResult.FullName);
            Assert.Null(ourResult.Secret);
        }

        // ──── Test 3: ForMember Ignore ───────────────────────────────

        [Fact]
        public void ForMember_Ignore_MatchesAutoMapper()
        {
            var source = new SimpleSource { Id = 10, Name = "IgnoreMe", Score = 5.5, IsActive = false };

            // Our library
            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<SimpleSource, SimpleDest>()
                    .ForMember(d => d.Name, opt => opt.Ignore());
            });
            var ourResult = ourMapper.Map<SimpleSource, SimpleDest>(source);

            // Real AutoMapper
            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
            {
                cfg.CreateMap<SimpleSource, SimpleDest>()
                    .ForMember<string>(d => d.Name, opt => opt.Ignore());
            });
            var realResult = realMapper.Map<SimpleSource, SimpleDest>(source);

            AssertEquivalent(realResult, ourResult, "ForMember_Ignore");
            Assert.Null(ourResult.Name);
            Assert.Equal(10, ourResult.Id);
        }

        // ──── Test 4: Nested Object Mapping ──────────────────────────

        [Fact]
        public void NestedObjectMapping_MatchesAutoMapper()
        {
            var source = new NestedSource
            {
                Id = 1,
                Inner = new InnerSource { Value = 99, Label = "inner" }
            };

            // Our library
            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<NestedSource, NestedDest>();
                cfg.CreateMap<InnerSource, InnerDest>();
            });
            var ourResult = ourMapper.Map<NestedSource, NestedDest>(source);

            // Real AutoMapper
            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
            {
                cfg.CreateMap<NestedSource, NestedDest>();
                cfg.CreateMap<InnerSource, InnerDest>();
            });
            var realResult = realMapper.Map<NestedSource, NestedDest>(source);

            AssertEquivalent(realResult, ourResult, "NestedObjectMapping");
            Assert.NotNull(ourResult.Inner);
            Assert.Equal(99, ourResult.Inner.Value);
            Assert.Equal("inner", ourResult.Inner.Label);
        }

        // ──── Test 5: Collection Mapping ─────────────────────────────

        [Fact]
        public void CollectionMapping_MatchesAutoMapper()
        {
            var source = new CollectionSource
            {
                Title = "Items",
                Items = new List<CollectionItemSource>
                {
                    new CollectionItemSource { Id = 1, Name = "A" },
                    new CollectionItemSource { Id = 2, Name = "B" },
                    new CollectionItemSource { Id = 3, Name = "C" }
                }
            };

            // Our library
            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<CollectionSource, CollectionDest>();
                cfg.CreateMap<CollectionItemSource, CollectionItemDest>();
            });
            var ourResult = ourMapper.Map<CollectionSource, CollectionDest>(source);

            // Real AutoMapper
            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
            {
                cfg.CreateMap<CollectionSource, CollectionDest>();
                cfg.CreateMap<CollectionItemSource, CollectionItemDest>();
            });
            var realResult = realMapper.Map<CollectionSource, CollectionDest>(source);

            AssertEquivalent(realResult, ourResult, "CollectionMapping");
            Assert.Equal(3, ourResult.Items.Count);
            Assert.Equal("A", ourResult.Items[0].Name);
        }

        // ──── Test 6: AfterMap Lambda ────────────────────────────────

        [Fact]
        public void AfterMapLambda_MatchesAutoMapper()
        {
            var source = new AfterMapSource { First = "Hello", Second = "World" };

            // Our library
            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<AfterMapSource, AfterMapDest>()
                    .AfterMap((src, dest) => dest.Combined = src.First + " " + src.Second);
            });
            var ourResult = ourMapper.Map<AfterMapSource, AfterMapDest>(source);

            // Real AutoMapper
            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
            {
                cfg.CreateMap<AfterMapSource, AfterMapDest>()
                    .AfterMap((src, dest) => dest.Combined = src.First + " " + src.Second);
            });
            var realResult = realMapper.Map<AfterMapSource, AfterMapDest>(source);

            AssertEquivalent(realResult, ourResult, "AfterMapLambda");
            Assert.Equal("Hello World", ourResult.Combined);
        }

        // ──── Test 7: AfterMap Action Class with DI ──────────────────

        [Fact]
        public void AfterMapActionClass_WithDI_Works()
        {
            var source = new AfterMapSource { First = "Hello", Second = "World" };
            var logger = new AfterMapLogger();

            var services = new ServiceCollection();
            services.AddSingleton(logger);
            services.AddSingleton<CombineFieldsAction>();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<AfterMapSource, AfterMapDest>()
                    .AfterMap<CombineFieldsAction>();
            });

            var provider = services.BuildServiceProvider();
            var ourMapper = provider.GetRequiredService<IMapper>();
            var ourResult = ourMapper.Map<AfterMapSource, AfterMapDest>(source);

            Assert.Equal("Hello | World", ourResult.Combined);
            Assert.True(logger.WasCalled, "AfterMapLogger was not called – DI resolution failed");
            Assert.Equal("CombineFieldsAction executed", logger.LastMessage);
        }

        // ──── Test 8: Null Source ────────────────────────────────────

        [Fact]
        public void NullSource_ReturnsNull_MatchesAutoMapper()
        {
            var ourMapper = CreateOurMapper(cfg => cfg.CreateMap<SimpleSource, SimpleDest>());
            var ourResult = ourMapper.Map<SimpleSource, SimpleDest>((SimpleSource)null);

            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
                cfg.CreateMap<SimpleSource, SimpleDest>());
            var realResult = realMapper.Map<SimpleSource, SimpleDest>((SimpleSource)null);

            Assert.Null(realResult);
            Assert.Null(ourResult);
        }

        [Fact]
        public void NullNestedProperty_MatchesAutoMapper()
        {
            var source = new NullableSource { Text = "hello", OptionalInt = null, Inner = null };

            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<NullableSource, NullableDest>();
                cfg.CreateMap<InnerSource, InnerDest>();
            });
            var ourResult = ourMapper.Map<NullableSource, NullableDest>(source);

            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
            {
                cfg.CreateMap<NullableSource, NullableDest>();
                cfg.CreateMap<InnerSource, InnerDest>();
            });
            var realResult = realMapper.Map<NullableSource, NullableDest>(source);

            AssertEquivalent(realResult, ourResult, "NullNestedProperty");
            Assert.Null(ourResult.Inner);
            Assert.Null(ourResult.OptionalInt);
        }

        // ──── Test 9: Map Into Existing Instance ─────────────────────

        [Fact]
        public void MapIntoExistingInstance_MatchesAutoMapper()
        {
            var source = new ExistingSource { Id = 5, Name = "Updated" };
            var existingOur = new ExistingDest { Id = 0, Name = "Old", PreExisting = "keep me" };
            var existingReal = new ExistingDest { Id = 0, Name = "Old", PreExisting = "keep me" };

            var ourMapper = CreateOurMapper(cfg =>
                cfg.CreateMap<ExistingSource, ExistingDest>());
            ourMapper.Map(source, existingOur);

            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
                cfg.CreateMap<ExistingSource, ExistingDest>());
            realMapper.MapToExisting(source, existingReal);

            AssertEquivalent(existingReal, existingOur, "MapIntoExistingInstance");
            Assert.Equal("keep me", existingOur.PreExisting);
            Assert.Equal(5, existingOur.Id);
            Assert.Equal("Updated", existingOur.Name);
        }

        [Fact]
        public void MapIntoExistingInstance_NullSource_DoesNotModify()
        {
            var existingOur = new ExistingDest { Id = 7, Name = "Original", PreExisting = "preserved" };
            var existingReal = new ExistingDest { Id = 7, Name = "Original", PreExisting = "preserved" };

            var ourMapper = CreateOurMapper(cfg =>
                cfg.CreateMap<ExistingSource, ExistingDest>());
            ourMapper.Map((ExistingSource)null, existingOur);

            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
                cfg.CreateMap<ExistingSource, ExistingDest>());
            realMapper.MapToExisting((ExistingSource)null, existingReal);

            AssertEquivalent(existingReal, existingOur, "MapIntoExisting_NullSource");
            Assert.Equal(7, existingOur.Id);
            Assert.Equal("Original", existingOur.Name);
            Assert.Equal("preserved", existingOur.PreExisting);
        }

        // ──── Test 10: DI Registration ───────────────────────────────

        [Fact]
        public void DI_Registration_ResolvesMapper()
        {
            var services = new ServiceCollection();
            services.AddAutoMapper(cfg => cfg.CreateMap<SimpleSource, SimpleDest>());

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();
            Assert.NotNull(mapper);

            var configProvider = provider.GetRequiredService<IConfigurationProvider>();
            Assert.NotNull(configProvider);

            var source = new SimpleSource { Id = 1, Name = "DI Test", Score = 3.14, IsActive = true };
            var result = mapper.Map<SimpleSource, SimpleDest>(source);
            Assert.Equal(1, result.Id);
            Assert.Equal("DI Test", result.Name);
            Assert.Equal(3.14, result.Score);
            Assert.True(result.IsActive);
        }

        // ──── Test 11: Multiple AfterMap Hooks ───────────────────────

        [Fact]
        public void MultipleAfterMapHooks_RunInOrder()
        {
            var source = new AfterMapSource { First = "A", Second = "B" };
            var callOrder = new List<string>();

            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<AfterMapSource, AfterMapDest>()
                    .AfterMap((src, dest) =>
                    {
                        callOrder.Add("first");
                        dest.Combined = src.First;
                    })
                    .AfterMap((src, dest) =>
                    {
                        callOrder.Add("second");
                        dest.Combined += " + " + src.Second;
                    });
            });

            var result = ourMapper.Map<AfterMapSource, AfterMapDest>(source);
            Assert.Equal(new List<string> { "first", "second" }, callOrder);
            Assert.Equal("A + B", result.Combined);
        }

        // ──── Test 12: Nested with Null Inner ────────────────────────

        [Fact]
        public void NestedNull_MatchesAutoMapper()
        {
            var source = new NestedSource { Id = 5, Inner = null };

            var ourMapper = CreateOurMapper(cfg =>
            {
                cfg.CreateMap<NestedSource, NestedDest>();
                cfg.CreateMap<InnerSource, InnerDest>();
            });
            var ourResult = ourMapper.Map<NestedSource, NestedDest>(source);

            var realMapper = RealAutoMapperFactory.CreateMapper(cfg =>
            {
                cfg.CreateMap<NestedSource, NestedDest>();
                cfg.CreateMap<InnerSource, InnerDest>();
            });
            var realResult = realMapper.Map<NestedSource, NestedDest>(source);

            AssertEquivalent(realResult, ourResult, "NestedNull");
            Assert.Null(ourResult.Inner);
        }

        // ──── Test 13: DI Assembly Scanning ──────────────────────────

        [Fact]
        public void DI_AssemblyScanning_FindsProfiles()
        {
            var services = new ServiceCollection();
            services.AddAutoMapper(typeof(MappingComparisonTests));

            var provider = services.BuildServiceProvider();
            var mapper = provider.GetRequiredService<IMapper>();
            Assert.NotNull(mapper);

            var source = new SimpleSource { Id = 100, Name = "ScanTest", Score = 1.0, IsActive = true };
            var result = mapper.Map<SimpleSource, SimpleDest>(source);
            Assert.Equal(100, result.Id);
        }
        // ──── Test 14: Dynamic Configuration with loggerFactory Object ───────────────

        [Fact]
        public void DynamicConfiguration_WithLoggerFactoryObject_Works()
        {
            var loggerFactory = new object(); // representing ILoggerFactory mock
            var configurableTypes = new[] { typeof(SimpleDest) };

            var ourConfig = new MapperConfiguration(cfg =>
            {
                foreach (var destType in configurableTypes)
                {
                    cfg.CreateMap(typeof(SimpleSource), destType);
                }
            }, loggerFactory);
            var ourMapper = ourConfig.CreateMapper();

            var source = new SimpleSource { Id = 77, Name = "Dynamic", Score = 4.2, IsActive = true };
            var ourResult = (SimpleDest)ourMapper.Map(source, typeof(SimpleSource), typeof(SimpleDest)); // using non-generic mapping to test it too, or generic

            Assert.Equal(77, ourResult.Id);
            Assert.Equal("Dynamic", ourResult.Name);
            Assert.Equal(4.2, ourResult.Score);
            Assert.True(ourResult.IsActive);
        }
    }

    // ────── Profile for assembly scanning tests ──────────────────────
    public class AssemblyTestProfile : Profile
    {
        public AssemblyTestProfile()
        {
            CreateMap<SimpleSource, SimpleDest>();
            CreateMap<InnerSource, InnerDest>();
        }
    }
}
