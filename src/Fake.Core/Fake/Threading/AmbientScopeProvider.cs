using System.Collections.Concurrent;

namespace Fake.Threading;

public class AmbientScopeProvider<T> : IAmbientScopeProvider<T>
{
    protected static readonly ConcurrentDictionary<string, ScopeItem?> ScopeDictionary = new();

    public virtual T? GetValue(string contextKey)
    {
        var item = GetCurrentItemOrNull(contextKey);
        return item == null ? default : item.Value;
    }

    public virtual IDisposable BeginScope(string contextKey, T value)
    {
        // 将需要临时存储的对象，用 ScopeItem 包装起来，它的外部对象是当前对象 (如果存在的话)。
        var item = new ScopeItem(value, GetCurrentItemOrNull(contextKey));

        if (!ScopeDictionary.TryAdd(item.Id, item))
        {
            throw new FakeException("无法添加item，ScopeDictionary.TryAdd返回false");
        }

        // 将上下文关联到当前作用域对象
        CallContext.SetData(contextKey, item.Id);

        return new DisposableWrapper(() =>
        {
            ScopeDictionary.TryRemove(item.Id, out item);

            // 将上下文重定向到外部对象，如果没有外部对象，说明离开上下文，直接关联 NULL。
            CallContext.SetData(contextKey, item!.Outer?.Id);
        });
    }


    protected ScopeItem? GetCurrentItemOrNull(string contextKey)
    {
        return CallContext.GetData(contextKey) is string scopeItemId ? ScopeDictionary.GetOrDefault(scopeItemId) : null;
    }


    protected class ScopeItem(T? value, ScopeItem? outer = null)
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        public ScopeItem? Outer { get; } = outer;

        public T? Value { get; } = value;
    }
}