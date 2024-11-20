using System.Collections.Concurrent;
using System.Reflection;

namespace Fake.Domain;

/// <summary>
/// 枚举基类
/// </summary>
public abstract class Enumeration(string name, int value, string? description = null) : IComparable
{
    public int Value { get; } = value;
    public string Name { get; } = name;

    public string? Description { get; } = description;

    private static readonly ConcurrentDictionary<Type, IEnumerable<object>> Cache = new();

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration enumeration) return false;

        if (GetType() != obj.GetType()) return false;

        return Value == enumeration.Value;
    }

    public static bool operator ==(Enumeration obj1, Enumeration? obj2)
    {
        return obj1.Equals(obj2);
    }

    public static bool operator !=(Enumeration obj1, Enumeration? obj2)
    {
        return !(obj1 == obj2);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }

    public int CompareTo(object? obj)
    {
        var enumeration = obj as Enumeration;
        return Value.CompareTo(enumeration?.Value);
    }

    #region Utils

    public static IEnumerable<T> GetAll<T>() where T : Enumeration
    {
        var type = typeof(T);
        return Cache.GetOrAdd(type, () =>
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(x => x.FieldType == type)
                .Select(x => x.GetValue(default)!);
        }).Cast<T>();
    }

    public static T FromValue<T>(int value) where T : Enumeration
    {
        return Parse<T, int>(value, "value", x => x.Value == value);
    }

    public static T FromName<T>(string displayName) where T : Enumeration
    {
        return Parse<T, string>(displayName, "name", x => x.Name == displayName);
    }


    private static T Parse<T, TV>(TV value, string description, Func<T, bool> predicate) where T : Enumeration
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);
        if (matchingItem is null)
        {
            throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");
        }

        return matchingItem;
    }

    #endregion
}