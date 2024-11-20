using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Fake.Helpers;

public static class ReflectionHelper
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertiesCaches = new();
    private static readonly ConcurrentDictionary<Assembly, IReadOnlyList<Type>> AssemblyCaches = new();

    /// <summary>
    /// 尝试为给定对象的属性赋值
    /// </summary>
    /// <param name="obj">给定对象</param>
    /// <param name="propertySelector">属性选择器</param>
    /// <param name="valueFactory">值工厂</param>
    /// <param name="ignoreAttributeTypes">忽略特性，如果属性标记了，则忽略赋值</param>
    public static void TrySetProperty<TObject, TValue>(TObject obj, Expression<Func<TObject, TValue>> propertySelector,
        Func<TValue> valueFactory, params Type[] ignoreAttributeTypes)
    {
        if (obj is null) return;

        var cacheKey = $"{obj.GetType().FullName}-{propertySelector}-{ignoreAttributeTypes.JoinAsString("-")}";

        var propertyInfo = PropertiesCaches.GetOrAdd(cacheKey, _ =>
        {
            // 必须从字段或属性上读取
            if (propertySelector.Body.NodeType != ExpressionType.MemberAccess) return null;

            var memberExpression = propertySelector.Body.To<MemberExpression>();

            var propertyInfo = obj.GetType().GetProperties().FirstOrDefault(x =>
                x.Name == memberExpression.Member.Name &&
                x.GetSetMethod(true) != null);

            if (propertyInfo == null) return null;

            // 如果定义了忽略特性
            if (ignoreAttributeTypes.Any(t => propertyInfo.IsDefined(t, true))) return null;

            return propertyInfo;
        });

        propertyInfo?.SetValue(obj, valueFactory());
    }

    /// <summary>
    /// 获取给定成员指定特性，如果没有则返回defaultValue
    /// </summary>
    /// <param name="memberInfo">给定成员</param>
    /// <param name="defaultValue">没找到的默认值</param>
    /// <param name="inherit">从该成员的继承链上找</param>
    /// <param name="includeDeclaringType">从定义类型上寻找</param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static TAttribute? GetAttributeOrNull<TAttribute>(MemberInfo memberInfo,
        TAttribute? defaultValue = null, bool inherit = true, bool includeDeclaringType = true)
        where TAttribute : Attribute
    {
        return memberInfo.GetCustomAttribute<TAttribute>(inherit)
               ?? memberInfo.DeclaringType?.GetType().GetCustomAttribute<TAttribute>(inherit)
               ?? defaultValue;
    }

    /// <summary>
    /// 获取给定成员指定特性集合
    /// </summary>
    /// <param name="memberInfo">给定成员</param>
    /// <param name="inherit">从该成员的继承链上找</param>
    /// <param name="includeDeclaringType">从定义类型上寻找</param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IEnumerable<TAttribute> GetAttributes<TAttribute>(MemberInfo memberInfo,
        bool inherit = true, bool includeDeclaringType = true)
        where TAttribute : Attribute
    {
        return memberInfo.GetCustomAttributes<TAttribute>(inherit)
            .Concat(memberInfo.DeclaringType?.GetType().GetCustomAttributes<TAttribute>(inherit)
                    ?? Array.Empty<TAttribute>());
    }


    /// <summary>
    /// 获取类型实例
    /// </summary>
    /// <param name="type"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static object? CreateInstance(Type type, params object?[] args)
    {
        //TODO: 可以优化
        return Activator.CreateInstance(type, args);
    }

    /// <summary>
    /// 获取成员类型
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static Type GetMemberType(this MemberInfo member)
    {
        if (member is PropertyInfo propertyInfo)
            return propertyInfo.PropertyType;
        if (member is MethodInfo methodInfo)
            return methodInfo.ReturnType;
        if (member is FieldInfo fieldInfo)
            return fieldInfo.FieldType;
        if ((object)member == null)
            throw new ArgumentNullException(nameof(member));
        throw new ArgumentOutOfRangeException(nameof(member));
    }

    public static IReadOnlyList<Type> GetAssemblyAllTypes(Assembly assembly)
    {
        return AssemblyCaches.GetOrAdd(assembly, _ => assembly.GetTypes());
    }
}