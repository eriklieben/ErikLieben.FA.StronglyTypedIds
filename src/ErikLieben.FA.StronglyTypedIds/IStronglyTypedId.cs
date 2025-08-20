namespace ErikLieben.FA.StronglyTypedIds;

public interface IStronglyTypedId<out T> where T : IEquatable<T>
{
    T Value { get; }
}