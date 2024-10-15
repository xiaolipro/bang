namespace Fake.Data;

public static class HasExtraPropertiesExtensions
{
    public static bool HasExtraProperty(this IHasExtraProperties source, string key)
    {
        return source.ExtraProperties.ContainsKey(key);
    }

    /// <summary>
    /// 添加额外属性
    /// </summary>
    /// <param name="source"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="key">key</paramref> is null.</exception>
    /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.Dictionary`2"></see>.</exception>
    public static IHasExtraProperties AddExtraProperty(this IHasExtraProperties source, string key, object? value)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        source.ExtraProperties[key] = value;
        return source;
    }

    /// <summary>
    /// 尝试添加额外属性，如果存在就跳过并返回false
    /// </summary>
    /// <param name="source"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="key">name</paramref> is null.</exception>
    public static bool TryAddExtraProperty(this IHasExtraProperties source, string key, object value)
    {
        ThrowHelper.ThrowIfNull(key, nameof(key));
        return source.ExtraProperties.TryAdd(key, value);
    }

    /// <summary>
    /// 获取额外属性，如果不存在则返回null
    /// </summary>
    /// <param name="source"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
    public static object? GetExtraProperty(this IHasExtraProperties source, string key)
    {
        source.ExtraProperties.TryGetValue(key, out var res);

        return res;
    }
    
    /// <summary>
    /// 获取额外属性，如果不存在则返回默认值
    /// </summary>
    /// <param name="source"></param>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetExtraProperty<T>(this IHasExtraProperties source, string key)
    {
        var res = source.GetExtraProperty(key);
        return res is null? default: (T)res;
    }
}