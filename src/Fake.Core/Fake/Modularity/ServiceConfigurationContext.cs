using Fake.Data;

namespace Fake.Modularity;

public class ServiceConfigurationContext(IServiceCollection services) : IHasExtraProperties
{
    public IServiceCollection Services { get; } = ThrowHelper.ThrowIfNull(services, nameof(services));

    /// <summary>
    /// 用于在模块之间共享数据
    /// </summary>
    public ExtraProperties ExtraProperties { get; } = new();
}