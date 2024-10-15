using System.Collections.Concurrent;

namespace Fake.Threading;

/// <summary>
/// 线程本地存储 缓存
/// </summary>
public static class CallContext
{
    private static readonly ConcurrentDictionary<string, AsyncLocal<object?>> Data = new();

    public static void SetData(string key, object? value)
    {
        Data.GetOrAdd(key, _ => new AsyncLocal<object?>()).Value = value;
    }

    public static object? GetData(string key)
    {
        return Data.GetOrAdd(key, _ => new AsyncLocal<object?>()).Value;
    }

    public static void FreeData(string key)
    {
        Data.TryRemove(key, out _);
    }
}