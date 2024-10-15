namespace Fake;

public class FakeApplicationCreationOptions(IServiceCollection services)
{
    public string ApplicationName { get; set; } = string.Empty;

    public IServiceCollection Services { get; } = ThrowHelper.ThrowIfNull(services, nameof(services));

    /// <summary>
    /// 仅当services中没有IConfiguration时，会根据此配置创建一个
    /// </summary>
    public FakeConfigurationBuilderOptions Configuration { get; } = new();
}