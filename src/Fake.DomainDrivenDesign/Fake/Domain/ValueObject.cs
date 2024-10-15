namespace Fake.Domain;

/// <summary>
/// 值对象
/// </summary>
public abstract class ValueObject
{
    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject entity) return false;
        if (GetType() != obj.GetType()) return false;

        return GetEqualityComponents().SequenceEqual(entity.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x == null ? 0 : x.GetHashCode())
            .Aggregate((x, y) => x ^ y);
    }

    protected abstract IEnumerable<object> GetEqualityComponents();

    public ValueObject Clone()
    {
        return MemberwiseClone().To<ValueObject>();
    }
}