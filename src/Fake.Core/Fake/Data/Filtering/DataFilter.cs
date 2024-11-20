using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Fake.Data.Filtering;

public class DataFilter(IServiceProvider serviceProvider) : IDataFilter
{
    private readonly ConcurrentDictionary<Type, object> _filters = new();

    public IDisposable Enable<TFilter>() where TFilter : ICanFilter
    {
        return GetFilter<TFilter>().Enable();
    }

    public IDisposable Disable<TFilter>() where TFilter : ICanFilter
    {
        return GetFilter<TFilter>().Disable();
    }

    public bool IsEnabled<TFilter>() where TFilter : ICanFilter
    {
        return GetFilter<TFilter>().IsEnabled;
    }

    /// <summary>
    /// 类型泛参转方法泛参
    /// </summary>
    /// <typeparam name="TFilter"></typeparam>
    /// <returns></returns>
    private IDataFilter<TFilter> GetFilter<TFilter>()
        where TFilter : ICanFilter
    {
        var filter = _filters.GetOrAdd(
            typeof(TFilter),
            valueFactory: () => serviceProvider.GetRequiredService<IDataFilter<TFilter>>());
        return filter.To<IDataFilter<TFilter>>();
    }
}

public class DataFilter<TFilter>(IOptions<FakeDataFilterOptions> options) : IDataFilter<TFilter>
    where TFilter : ICanFilter
{
    public bool IsEnabled
    {
        get
        {
            EnsureInitialized();
            return _filter.Value!.IsEnabled;
        }
    }

    private readonly FakeDataFilterOptions _options = options.Value;

    private readonly AsyncLocal<DataFilterState> _filter = new();

    public IDisposable Enable()
    {
        if (IsEnabled)
        {
            return NullDisposable.Instance;
        }

        _filter.Value!.IsEnabled = true;

        return new DisposableWrapper(() => Disable());
    }

    public IDisposable Disable()
    {
        if (!IsEnabled)
        {
            return NullDisposable.Instance;
        }

        _filter.Value!.IsEnabled = false;

        return new DisposableWrapper(() => Enable());
    }

    private void EnsureInitialized()
    {
        if (_filter.Value != null)
        {
            return;
        }

        // tips：这里要克隆，不然会影响到默认值
        _filter.Value = _options.GetDataFilterStateOrNull<TFilter>()?.Clone() ??
                        new DataFilterState(_options.IsDefaultEnabled);
    }
}