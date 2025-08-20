namespace ErikLieben.FA.StronglyTypedIds.Tests;

public class IStronglyTypedIdTests
{
    private sealed record GuidId(Guid Value) : StronglyTypedId<Guid>(Value);

    public class Value
    {
        [Fact]
        public void Should_return_value_assigned_in_constructor()
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
