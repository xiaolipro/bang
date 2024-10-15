using Fake.Helpers;

namespace Fake.Reflection;

public class FakeTypeScanner : IFakeTypeScanner
{
    private readonly IFakeAssemblyScanner _fakeAssemblyScanner;

    private readonly Lazy<IReadOnlyList<Type>> _types;

    public FakeTypeScanner(IFakeAssemblyScanner fakeAssemblyScanner)
    {
        _fakeAssemblyScanner = fakeAssemblyScanner;

        _types = new Lazy<IReadOnlyList<Type>>(FindAllTypes, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyList<Type> Scan()
    {
        return _types.Value;
    }

    private IReadOnlyList<Type> FindAllTypes()
    {
        var allTypes = new List<Type>();

        foreach (var assembly in _fakeAssemblyScanner.Scan())
        {
            try
            {
                var types = ReflectionHelper.GetAssemblyAllTypes(assembly);

                if (!types.Any())
                {
                    continue;
                }

                allTypes.AddRange(types.Where(type => type != null));
            }
            catch
            {
                //TODO: Trigger a global event?
            }
        }

        return allTypes;
    }
}