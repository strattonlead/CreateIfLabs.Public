using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoMapper;

namespace Benchmark
{
    public class SourceModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public NestedSource Nested { get; set; }
    }

    public class DestModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public NestedDest Nested { get; set; }
    }

    public class NestedSource
    {
        public string Details { get; set; }
        public int Value { get; set; }
    }

    public class NestedDest
    {
        public string Details { get; set; }
        public int Value { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<SourceModel, DestModel>();
                cfg.CreateMap<NestedSource, NestedDest>();
            });
            var mapper = config.CreateMapper();

            // Warmup
            var singleSource = new SourceModel { Id = 1, Name = "Test", CreatedAt = DateTime.UtcNow, Price = 9.99m, IsActive = true, Nested = new NestedSource { Details = "test", Value = 1 } };
            var warmupDest = mapper.Map<SourceModel, DestModel>(singleSource);

            // Generate 1 million
            int count = 1_000_000;
            var sources = new List<SourceModel>(count);
            for (int i = 0; i < count; i++)
            {
                sources.Add(new SourceModel
                {
                    Id = i,
                    Name = "Item " + i,
                    CreatedAt = DateTime.UtcNow,
                    Price = i * 1.5m,
                    IsActive = i % 2 == 0,
                    Nested = new NestedSource { Details = "Nested " + i, Value = i * 10 }
                });
            }

            // Benchmark
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var sw = Stopwatch.StartNew();

            var dests = mapper.Map<List<SourceModel>, List<DestModel>>(sources);

            sw.Stop();
            Console.WriteLine($"Mapped {dests.Count} records in {sw.ElapsedMilliseconds} ms.");
        }
    }
}
