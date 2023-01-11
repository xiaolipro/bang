﻿using Bang.Helpers;

namespace Bang.Reflection;

public interface ITypeScanner
{
    IReadOnlyList<Type> Scan();
}

public class BangAssemblyTypeScanner: ITypeScanner
{
    private readonly IAssemblyScanner _assemblyScanner;

    private readonly Lazy<IReadOnlyList<Type>> _types;
    
    public BangAssemblyTypeScanner(IAssemblyScanner assemblyScanner)
    {
        _assemblyScanner = assemblyScanner;

        _types = new Lazy<IReadOnlyList<Type>>(FindAllTypes, LazyThreadSafetyMode.ExecutionAndPublication);
    }
    public IReadOnlyList<Type> Scan()
    {
        return _types.Value;
    }
    
    private IReadOnlyList<Type> FindAllTypes()
    {
        var allTypes = new List<Type>();

        foreach (var assembly in _assemblyScanner.Scan())
        {
            try
            {
                var types = AssemblyHelper.GetAllTypes(assembly);

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