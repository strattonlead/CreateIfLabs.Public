namespace CreateIfLabs.AutoMapper.Tests.Models
{
    // ─── Simple flat mapping ──────────────────────────────────────
    public class SimpleSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Score { get; set; }
        public bool IsActive { get; set; }
    }

    public class SimpleDest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Score { get; set; }
        public bool IsActive { get; set; }
    }

    // ─── ForMember / MapFrom / Ignore ─────────────────────────────
    public class ForMemberSource
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string Secret { get; set; }
    }

    public class ForMemberDest
    {
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Secret { get; set; }
        public string Extra { get; set; }
    }

    // ─── Nested object mapping ────────────────────────────────────
    public class InnerSource
    {
        public int Value { get; set; }
        public string Label { get; set; }
    }

    public class InnerDest
    {
        public int Value { get; set; }
        public string Label { get; set; }
    }

    public class NestedSource
    {
        public int Id { get; set; }
        public InnerSource Inner { get; set; }
    }

    public class NestedDest
    {
        public int Id { get; set; }
        public InnerDest Inner { get; set; }
    }

    // ─── Collection mapping ───────────────────────────────────────
    public class CollectionItemSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CollectionItemDest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CollectionSource
    {
        public string Title { get; set; }
        public System.Collections.Generic.List<CollectionItemSource> Items { get; set; }
    }

    public class CollectionDest
    {
        public string Title { get; set; }
        public System.Collections.Generic.List<CollectionItemDest> Items { get; set; }
    }

    // ─── AfterMap testing ─────────────────────────────────────────
    public class AfterMapSource
    {
        public string First { get; set; }
        public string Second { get; set; }
    }

    public class AfterMapDest
    {
        public string First { get; set; }
        public string Second { get; set; }
        public string Combined { get; set; }
    }

    // ─── Null propagation ─────────────────────────────────────────
    public class NullableSource
    {
        public string Text { get; set; }
        public int? OptionalInt { get; set; }
        public InnerSource Inner { get; set; }
    }

    public class NullableDest
    {
        public string Text { get; set; }
        public int? OptionalInt { get; set; }
        public InnerDest Inner { get; set; }
    }

    // ─── Existing instance mapping ────────────────────────────────
    public class ExistingSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ExistingDest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PreExisting { get; set; }
    }
}
