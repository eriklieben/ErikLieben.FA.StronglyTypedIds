namespace ErikLieben.FA.StronglyTypedIds;

public abstract record StronglyTypedId<T>(T Value) : IStronglyTypedId<T> where T : IEquatable<T>
{
    public override string ToString() => Value?.ToString() ?? string.Empty;
}
