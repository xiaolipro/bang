using System.Collections.Immutable;
using System.Reflection;
using Fake.Modularity;

namespace Fake.Reflection;

/// <summary>
/// 扫描所有Fake模块相关的程序集
/// </summary>
public class FakeAssemblyScanner : IFakeAssemblyScanner
{
    private readonly IModuleContainer _moduleContainer;
    private readonly Lazy<IReadOnlyList<Assembly>> _assemblies;

    public FakeAssemblyScanner(IModuleContainer moduleContainer)
    {
        _moduleContainer = moduleContainer;

        _assemblies = new Lazy<IReadOnlyList<Assembly>>(FindAllFakeAssemblies, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyList<Assembly> Scan()
    {
        return _assemblies.Value;
    }

    private IReadOnlyList<Assembly> FindAllFakeAssemblies()
    {
        var assemblies = new List<Assembly>();

        foreach (var module in _moduleContainer.Modules)
        {
            assemblies.Add(module.Type.Assembly);
        }

        return assemblies.Distinct().ToImmutableList();
    }
}