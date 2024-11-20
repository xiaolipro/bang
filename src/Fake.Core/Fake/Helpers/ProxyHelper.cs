using System.Reflection;

namespace Fake.Helpers;

public static class ProxyHelper
{
    private const string CastleProxyNamespace = "Castle.Proxies";

    public static Type? GetUnProxiedType(object obj)
    {
        var type = obj.GetType();
        if (type.Namespace != CastleProxyNamespace) return type;

        var target = UnProxy(obj);
        if (target == obj)
        {
            return type.GetTypeInfo().BaseType;
        }

        return obj.GetType();
    }

    
    /// <summary>
    /// 如果是代理对象则返回动态代理目标对象，否则直接返回对象，支持Castle动态代理
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static object UnProxy(object obj)
    {
        if (obj.GetType().Namespace != CastleProxyNamespace) return obj;

        var targetField = obj.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(f => f.Name == "__target");

        if (targetField == null) return obj;

        return targetField.GetValue(obj)!;
    }
}