namespace ErikLieben.FA.StronglyTypedIds.Tests;

public class GenerateStronglyTypedIdSupportAttributeTests
{
    public class Defaults
    {
        [Fact]
        public void Should_have_all_flags_enabled_by_default()
        {
            // Arrange
            var sut = new GenerateStronglyTypedIdSupportAttribute();

            // Act
            var defaults = new
            {
                sut.GenerateJsonConverter,
                sut.GenerateTypeConverter,
                sut.GenerateParseMethod,
                sut.GenerateTryParseMethod,
                sut.GenerateComparisons,
                sut.GenerateNewMethod,
                sut.GenerateExtensions
            };

            // Assert
            Assert.True(defaults.GenerateJsonConverter);
            Assert.True(defaults.GenerateTypeConverter);
            Assert.True(defaults.GenerateParseMethod);
            Assert.True(defaults.GenerateTryParseMethod);
            Assert.True(defaults.GenerateComparisons);
            Assert.True(defaults.GenerateNewMethod);
            Assert.True(defaults.GenerateExtensions);
        }

        [Fact]
        public void Should_allow_overriding_flags()
        {
            // Arrange
            var sut = new GenerateStronglyTypedIdSupportAttribute
            {
                GenerateJsonConverter = false,
                GenerateTypeConverter = false,
                GenerateParseMethod = false,
                GenerateTryParseMethod = false,
                GenerateComparisons = false,
                GenerateNewMethod = false,
                GenerateExtensions = false
            };

            // Act
            var values = new[]
            {
                sut.GenerateJsonConverter,
                sut.GenerateTypeConverter,
                sut.GenerateParseMethod,
                sut.GenerateTryParseMethod,
                sut.GenerateComparisons,
                sut.GenerateNewMethod,
                sut.GenerateExtensions
            };

            // Assert
            Assert.All(values, v => Assert.False(v));
        }
    }

    public class AttributeUsage
    {
        [Fact]
        public void Should_target_classes_and_structs_only()
        {
            // Arrange
            var usage = Attribute
                .GetCustomAttributes(typeof(GenerateStronglyTypedIdSupportAttribute))
                .OfType<AttributeUsageAttribute>()
                .SingleOrDefault();

            // Act
            var targets = usage!.ValidOn;

            // Assert
            Assert.NotNull(usage);
            Assert.True((targets & AttributeTargets.Class) == AttributeTargets.Class);
            Assert.True((targets & AttributeTargets.Struct) == AttributeTargets.Struct);
            Assert.False((targets & AttributeTargets.Method) == AttributeTargets.Method);
            Assert.False((targets & AttributeTargets.Property) == AttributeTargets.Property);
            Assert.False((targets & AttributeTargets.Parameter) == AttributeTargets.Parameter);
        }
    }
}
