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
            Assert.Contains("public static bool TryParse(string? value, out OrderId result)", partial);
            Assert.Contains("public static OrderId New() => new(Guid.NewGuid())", partial);
            // Comparison operators for comparable Guid
            Assert.Contains("public static bool operator <(OrderId left, OrderId right)", partial);
            Assert.Contains("public static bool operator >=(OrderId left, OrderId right)", partial);
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
            Assert.Contains("public static bool TryParse(string? value, out CustomerId result)", partial);
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
