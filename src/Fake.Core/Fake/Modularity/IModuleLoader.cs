namespace Fake.Modularity;

/// <summary>
/// 模块加载器
/// </summary>
public interface IModuleLoader
{
    /// <summary>
    /// 根据启动模块加载所有依赖模块，返回topology序列
    /// </summary>
    /// <param name="services"></param>
    /// <param name="startupModuleType">启动模块类型</param>
    /// <returns></returns>
    /// <exception cref="FakeException">如果依赖图中存在环，则抛出异常</exception>
    IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType);
}