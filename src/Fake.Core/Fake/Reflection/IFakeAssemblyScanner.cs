using System.Reflection;

namespace Fake.Reflection;

/// <summary>
/// 程序集扫描器
/// </summary>
public interface IFakeAssemblyScanner
{
    IReadOnlyList<Assembly> Scan();
}