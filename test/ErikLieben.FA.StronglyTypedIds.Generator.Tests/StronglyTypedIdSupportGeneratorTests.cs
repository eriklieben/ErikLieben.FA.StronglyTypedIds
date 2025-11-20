using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ErikLieben.FA.StronglyTypedIds.Generator.Tests;

public class StronglyTypedIdSupportGeneratorTests
{
    public class Generate
    {
        [Fact]
        public void Should_generate_support_for_guid_based_id()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct OrderId(System.Guid Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert
            var trees = updatedCompilation.SyntaxTrees.Select(t => t.FilePath).ToArray();
            Assert.Contains(trees, p => p.EndsWith("OrderId.Support.g.cs", StringComparison.Ordinal));
            Assert.Contains(trees, p => p.EndsWith("OrderId.Partial.g.cs", StringComparison.Ordinal));

            var support = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("OrderId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("OrderId.Partial.g.cs", StringComparison.Ordinal)).ToString();

            // JSON converter class present with Read/Write
            Assert.Contains("public sealed class OrderIdJsonConverter", support);
            Assert.Contains("public override OrderId Read(ref Utf8JsonReader", support);
            Assert.Contains("public override void Write(Utf8JsonWriter writer, OrderId value", support);
            // Type converter present
            Assert.Contains("public sealed class OrderIdTypeConverter", support);
            // Extensions present
            Assert.Contains("public static class OrderIdExtensions", support);
            Assert.Contains("ToValues(this IEnumerable<OrderId> ids)", support);
            Assert.Contains("IsEmpty(this OrderId id)", support);
            Assert.Contains("IsNotEmpty(this OrderId id)", support);

            // Partial record includes attributes and methods
            Assert.Contains("[JsonConverter(typeof(OrderIdJsonConverter))]", partial);
            Assert.Contains("[TypeConverter(typeof(OrderIdTypeConverter))]", partial);
            Assert.Contains("public static OrderId From(string value)", partial);
            Assert.Contains("public static bool TryParse(string? value, out OrderId? result)", partial);
            Assert.Contains("public static OrderId New() => new(Guid.NewGuid())", partial);
            // Comparison operators for comparable Guid
            Assert.Contains("public static bool operator <(OrderId left, OrderId right)", partial);
            Assert.Contains("public static bool operator >=(OrderId left, OrderId right)", partial);
            // IComparable<T> interface
            Assert.Contains("public int CompareTo(OrderId? other)", partial);
        }

        [Fact]
        public void Should_generate_support_for_string_based_id()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct CustomerId(string Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert
            var support = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("CustomerId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("CustomerId.Partial.g.cs", StringComparison.Ordinal)).ToString();

            // JSON converter uses string read/write
            Assert.Contains("public sealed class CustomerIdJsonConverter", support);
            Assert.Contains("writer.WriteStringValue(value.Value)", support);

            // Type converter supports string
            Assert.Contains("public sealed class CustomerIdTypeConverter", support);

            // Extensions exist but no IsEmpty for string
            Assert.Contains("public static class CustomerIdExtensions", support);
            Assert.DoesNotContain("IsEmpty(this CustomerId id)", support);

            // Partial: TryParse should be trivial assignment for string
            Assert.Contains("public static bool TryParse(string? value, out CustomerId? result)", partial);
            Assert.Contains("result = new CustomerId(value);", partial);
            Assert.Contains("return true;", partial);
            // New uses Guid.NewGuid().ToString()
            Assert.Contains("public static CustomerId New() => new(Guid.NewGuid().ToString())", partial);
        }

        [Fact]
        public void Should_generate_support_for_int_and_long_based_ids()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSources = new[] {
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct IntId(int Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct LongId(long Value); }"
            };
            var compilation = CreateCompilation(new[] { attributeSource }.Concat(idSources).ToArray());
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert int
            var intSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("IntId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var intPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("IntId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetInt32()", intSupport);
            Assert.Contains("writer.WriteNumberValue(value.Value)", intSupport);
            Assert.Contains("int.TryParse(value, out var parsedValue)", intPartial);
            Assert.Contains("public static IntId New() => new(Random.Shared.Next())", intPartial);
            Assert.DoesNotContain("IsEmpty(this IntId id)", intSupport);

            // Assert long
            var longSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("LongId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var longPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("LongId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetInt64()", longSupport);
            Assert.Contains("writer.WriteNumberValue(value.Value)", longSupport);
            Assert.Contains("long.TryParse(value, out var parsedValue)", longPartial);
            Assert.Contains("public static LongId New() => new(Random.Shared.NextInt64())", longPartial);
            Assert.DoesNotContain("IsEmpty(this LongId id)", longSupport);
        }

        [Fact]
        public void Should_generate_support_for_datetime_and_offset_based_ids()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSources = new[] {
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct CreatedAt(System.DateTime Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct ModifiedAt(System.DateTimeOffset Value); }"
            };
            var compilation = CreateCompilation(new[] { attributeSource }.Concat(idSources).ToArray());
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // DateTime
            var dtSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("CreatedAt.Support.g.cs", StringComparison.Ordinal)).ToString();
            var dtPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("CreatedAt.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetDateTime()", dtSupport);
            Assert.Contains("writer.WriteStringValue(value.Value)", dtSupport);
            Assert.Contains("DateTime.TryParse(value, out var parsedValue)", dtPartial);
            Assert.Contains("public static CreatedAt New() => new(DateTime.UtcNow)", dtPartial);
            Assert.Contains("IsEmpty(this CreatedAt id)", dtSupport);

            // DateTimeOffset
            var dtoSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("ModifiedAt.Support.g.cs", StringComparison.Ordinal)).ToString();
            var dtoPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("ModifiedAt.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetDateTimeOffset()", dtoSupport);
            Assert.Contains("writer.WriteStringValue(value.Value)", dtoSupport);
            Assert.Contains("DateTimeOffset.TryParse(value, out var parsedValue)", dtoPartial);
            Assert.Contains("public static ModifiedAt New() => new(DateTimeOffset.UtcNow)", dtoPartial);
            Assert.Contains("IsNotEmpty(this ModifiedAt id)", dtoSupport);
        }

        [Fact]
        public void Should_honor_attribute_switches_when_disabled()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=false; public bool GenerateTypeConverter {get;set;}=false; public bool GenerateParseMethod {get;set;}=false; public bool GenerateTryParseMethod {get;set;}=false; public bool GenerateComparisons {get;set;}=false; public bool GenerateNewMethod {get;set;}=false; public bool GenerateExtensions {get;set;}=false; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport(GenerateJsonConverter=false,GenerateTypeConverter=false,GenerateParseMethod=false,GenerateTryParseMethod=false,GenerateComparisons=false,GenerateNewMethod=false,GenerateExtensions=false)] public readonly record struct DisabledId(System.Guid Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert: Partial record should be generated without attributes or methods, support file exists but empty of converters/extensions
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DisabledId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            var support = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DisabledId.Support.g.cs", StringComparison.Ordinal)).ToString();
            Assert.DoesNotContain("JsonConverter", partial);
            Assert.DoesNotContain("TypeConverter", partial);
            Assert.DoesNotContain("public static DisabledId From(string value)", partial);
            Assert.DoesNotContain("public static bool TryParse", partial);
            Assert.DoesNotContain("public static DisabledId New()", partial);
            Assert.DoesNotContain("operator <", partial);
            Assert.DoesNotContain("JsonConverter", support);
            Assert.DoesNotContain("TypeConverter", support);
            Assert.DoesNotContain("static class DisabledIdExtensions", support);
        }

        [Fact]
        public void Should_generate_support_for_decimal_short_byte_double_float_types()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSources = new[] {
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct DecimalId(decimal Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct ShortId(short Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct ByteId(byte Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct DoubleId(double Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct FloatId(float Value); }"
            };
            var compilation = CreateCompilation(new[] { attributeSource }.Concat(idSources).ToArray());
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert decimal
            var decimalSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DecimalId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var decimalPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DecimalId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetDecimal()", decimalSupport);
            Assert.Contains("writer.WriteNumberValue(value.Value)", decimalSupport);
            Assert.Contains("decimal.TryParse(value, out var parsedValue)", decimalPartial);

            // Assert short
            var shortSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("ShortId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var shortPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("ShortId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetInt16()", shortSupport);
            Assert.Contains("short.TryParse(value, out var parsedValue)", shortPartial);

            // Assert byte
            var byteSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("ByteId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var bytePartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("ByteId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetByte()", byteSupport);
            Assert.Contains("byte.TryParse(value, out var parsedValue)", bytePartial);

            // Assert double
            var doubleSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DoubleId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var doublePartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DoubleId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetDouble()", doubleSupport);
            Assert.Contains("double.TryParse(value, out var parsedValue)", doublePartial);

            // Assert float
            var floatSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("FloatId.Support.g.cs", StringComparison.Ordinal)).ToString();
            var floatPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("FloatId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetSingle()", floatSupport);
            Assert.Contains("float.TryParse(value, out var parsedValue)", floatPartial);
        }

        [Fact]
        public void Should_generate_static_empty_property_for_guid_and_datetime_types()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSources = new[] {
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct GuidId(Guid Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct DateId(DateTime Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct OffsetId(DateTimeOffset Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct IntId(int Value); }"
            };
            var compilation = CreateCompilation(new[] { attributeSource }.Concat(idSources).ToArray());
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert Empty property exists for Guid, DateTime, DateTimeOffset
            var guidPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("GuidId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("public static GuidId Empty { get; } = new(Guid.Empty)", guidPartial);

            var datePartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DateId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("public static DateId Empty { get; } = new(DateTime.MinValue)", datePartial);

            var offsetPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("OffsetId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("public static OffsetId Empty { get; } = new(DateTimeOffset.MinValue)", offsetPartial);

            // Assert Empty property does NOT exist for int
            var intPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("IntId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.DoesNotContain("public static IntId Empty", intPartial);
        }

        [Fact]
        public void Should_generate_implicit_and_explicit_conversion_operators()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct UserId(Guid Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("UserId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("public static implicit operator UserId(Guid value) => new(value)", partial);
            Assert.Contains("public static explicit operator Guid(UserId value) => value.Value", partial);
        }

        [Fact]
        public void Should_generate_icomparable_interface_and_compareto_method()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct OrderId(int Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("OrderId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains(": IComparable<OrderId>", partial);
            Assert.Contains("public int CompareTo(OrderId? other)", partial);
            Assert.Contains("if (other is null) return 1;", partial);
            Assert.Contains("return Comparer<int>.Default.Compare(Value, other.Value)", partial);
        }

        [Fact]
        public void Should_generate_debuggerdisplay_attribute()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct ProductId(int Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("ProductId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("[DebuggerDisplay(\"{Value}\")]", partial);
            Assert.Contains("using System.Diagnostics;", partial);
        }

        [Fact]
        public void Should_generate_format_provider_overloads_for_numeric_types()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSources = new[] {
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct DecimalId(decimal Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct DateId(DateTime Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct StringId(string Value); }"
            };
            var compilation = CreateCompilation(new[] { attributeSource }.Concat(idSources).ToArray());
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert format provider overload exists for decimal
            var decimalPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DecimalId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("public static DecimalId From(string value, IFormatProvider? provider)", decimalPartial);
            Assert.Contains("decimal.Parse(value, provider)", decimalPartial);
            Assert.Contains("public static bool TryParse(string? value, IFormatProvider? provider, out DecimalId? result)", decimalPartial);
            Assert.Contains("decimal.TryParse(value, provider, out var parsedValue)", decimalPartial);

            // Assert format provider overload exists for DateTime
            var datePartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DateId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("public static DateId From(string value, IFormatProvider? provider)", datePartial);
            Assert.Contains("DateTime.Parse(value, provider)", datePartial);
            Assert.Contains("public static bool TryParse(string? value, IFormatProvider? provider, out DateId? result)", datePartial);
            Assert.Contains("DateTime.TryParse(value, provider, System.Globalization.DateTimeStyles.None, out var parsedValue)", datePartial);

            // Assert format provider overload does NOT exist for string
            var stringPartial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("StringId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.DoesNotContain("IFormatProvider", stringPartial);
        }

        [Fact]
        public void Should_generate_conversion_helper_for_unsupported_types()
        {
            // Arrange - using a custom type that doesn't have built-in TryParse
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var customTypeSource = @"namespace Demo { public struct CustomType { public int Value; public override string ToString() => Value.ToString(); } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct CustomId(CustomType Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, customTypeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert ConversionHelper is generated in support file
            var support = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("CustomId.Support.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("internal static class ConversionHelper", support);
            Assert.Contains("internal static bool TryConvert<T>(string value, out T result)", support);
            Assert.Contains("result = (T)Convert.ChangeType(value, typeof(T));", support);
            Assert.Contains("return true;", support);

            // Assert TryParse uses ConversionHelper for custom type
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("CustomId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("ConversionHelper.TryConvert<CustomType>(value, out var parsedValue)", partial);
        }

        [Fact]
        public void Should_use_builtin_tryparse_for_supported_types_not_conversion_helper()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct IntId(int Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert ConversionHelper is NOT generated for supported types
            var support = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("IntId.Support.g.cs", StringComparison.Ordinal)).ToString();
            Assert.DoesNotContain("ConversionHelper", support);
            Assert.DoesNotContain("TryConvert", support);

            // Assert TryParse uses built-in int.TryParse
            var partial = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("IntId.Partial.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("int.TryParse(value, out var parsedValue)", partial);
            Assert.DoesNotContain("ConversionHelper", partial);
        }

        [Fact]
        public void Should_extract_record_info_with_various_namespaces()
        {
            // Arrange - test records in different namespace structures
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSources = new[] {
                "namespace MyCompany.Domain { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct UserId(Guid Value); }",
                "namespace Global { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct OrderId(int Value); }",
                "using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct ProductId(long Value);"
            };
            var compilation = CreateCompilation(new[] { attributeSource }.Concat(idSources).ToArray());
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert - files are generated with correct namespaces
            var userIdPartial = updatedCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("UserId.Partial.g.cs", StringComparison.Ordinal));
            Assert.NotNull(userIdPartial);
            Assert.Contains("namespace MyCompany.Domain", userIdPartial.ToString());

            var orderIdPartial = updatedCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("OrderId.Partial.g.cs", StringComparison.Ordinal));
            Assert.NotNull(orderIdPartial);
            Assert.Contains("namespace Global", orderIdPartial.ToString());

            var productIdPartial = updatedCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("ProductId.Partial.g.cs", StringComparison.Ordinal));
            Assert.NotNull(productIdPartial);
            // No namespace means global namespace - check it doesn't have namespace declaration
            Assert.DoesNotContain("namespace ", productIdPartial.ToString());
        }

        [Fact]
        public void Should_handle_various_system_type_name_conversions()
        {
            // Arrange - test all supported system types to ensure GetCSharpTypeName works
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSources = new[] {
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct GuidId(System.Guid Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct Int32Id(System.Int32 Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct Int64Id(System.Int64 Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct DecimalId(System.Decimal Value); }",
                "namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct StringId(System.String Value); }"
            };
            var compilation = CreateCompilation(new[] { attributeSource }.Concat(idSources).ToArray());
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert - type names are converted to C# aliases in generated code
            var guidSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("GuidId.Support.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetGuid()", guidSupport);
            Assert.DoesNotContain("System.Guid", guidSupport);

            var int32Support = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("Int32Id.Support.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetInt32()", int32Support);
            Assert.DoesNotContain("System.Int32", int32Support);

            var decimalSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("DecimalId.Support.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetDecimal()", decimalSupport);
            Assert.Contains("writer.WriteNumberValue(value.Value)", decimalSupport);

            var stringSupport = updatedCompilation.SyntaxTrees.First(t => t.FilePath.EndsWith("StringId.Support.g.cs", StringComparison.Ordinal)).ToString();
            Assert.Contains("reader.GetString()", stringSupport);
        }

        [Fact]
        public void Should_handle_record_with_readonly_modifier()
        {
            // Arrange
            var attributeSource = @"using System; namespace ErikLieben.FA.StronglyTypedIds { [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)] public class GenerateStronglyTypedIdSupportAttribute : Attribute { public bool GenerateJsonConverter {get;set;}=true; public bool GenerateTypeConverter {get;set;}=true; public bool GenerateParseMethod {get;set;}=true; public bool GenerateTryParseMethod {get;set;}=true; public bool GenerateComparisons {get;set;}=true; public bool GenerateNewMethod {get;set;}=true; public bool GenerateExtensions {get;set;}=true; } }";
            var idSource = @"namespace Demo { using System; using ErikLieben.FA.StronglyTypedIds; [GenerateStronglyTypedIdSupport] public readonly record struct TestId(Guid Value); }";
            var compilation = CreateCompilation(new[] { attributeSource, idSource });
            var generator = new StronglyTypedIdSupportGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var _);

            // Assert - generation succeeds with readonly record struct
            var partial = updatedCompilation.SyntaxTrees.FirstOrDefault(t => t.FilePath.EndsWith("TestId.Partial.g.cs", StringComparison.Ordinal));
            Assert.NotNull(partial);
            var partialContent = partial.ToString();
            Assert.Contains("partial record", partialContent);
            Assert.Contains("TestId", partialContent);
            Assert.Contains("public static TestId From(string value)", partialContent);
        }

        private static Compilation CreateCompilation(string[] sources)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
                        var trees = sources.Select(s => CSharpSyntaxTree.ParseText(s, parseOptions));

            // Basic references for the input compilation; we don't need to compile generated code here
            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Diagnostics.Debug).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.IsExternalInit).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Text.Json.JsonSerializer).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.TypeConverter).Assembly.Location),
            };

            return CSharpCompilation.Create(
                assemblyName: "Tests",
                syntaxTrees: trees,
                references: refs,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
