namespace ErikLieben.FA.StronglyTypedIds.Tests;

public class StronglyTypedIdTests
{
    // Simple concrete implementations for testing the abstract record
    private sealed record GuidId(Guid Value) : StronglyTypedId<Guid>(Value)
    {
        public override string ToString() => base.ToString();
    }
    private sealed record StringId(string? Value) : StronglyTypedId<string>(Value!)
    {
        public override string ToString() => base.ToString();
    }

    public new class ToString
    {
        [Fact]
        public void Should_return_value_tostring()
        {
            // Arrange
            var id = Guid.NewGuid();
            var sut = new GuidId(id);

            // Act
            var str = sut.ToString();

            // Assert
            Assert.Equal(id.ToString(), str);
        }

        [Fact]
        public void Should_return_empty_string_when_value_null()
        {
            // Arrange
            var sut = new StringId(null);

            // Act
            var str = sut.ToString();

            // Assert
            Assert.Equal(string.Empty, str);
        }
    }

    public class Equality
    {
        [Fact]
        public void Should_be_equal_when_values_are_equal()
        {
            // Arrange
            var id = Guid.NewGuid();
            var sut = new GuidId(id);
            var other = new GuidId(id);

            // Act
            var equals = sut.Equals(other);

            // Assert
            Assert.True(equals);
            Assert.Equal(sut.GetHashCode(), other.GetHashCode());
        }

        [Fact]
        public void Should_not_be_equal_when_values_differ()
        {
            // Arrange
            var sut = new GuidId(Guid.NewGuid());
            var other = new GuidId(Guid.NewGuid());

            // Act
            var equals = sut.Equals(other);

            // Assert
            Assert.False(equals);
        }
    }

    public class Interface
    {
        [Fact]
        public void Should_expose_value_via_interface()
        {
            // Arrange
            var id = Guid.NewGuid();
            IStronglyTypedId<Guid> sut = new GuidId(id);

            // Act
            var value = sut.Value;

            // Assert
            Assert.Equal(id, value);
        }
    }
}
