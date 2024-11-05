using System.Linq.Expressions;
using System.Reflection;
using Fake;

namespace System.Linq;

public static class FakeQueryableExtensions
{
    /// <summary>
    /// 如果<paramref name="condition"/> is true，按照<paramref name="predicate"/>过滤 <see cref="IQueryable{T}"/>
    /// </summary>
    /// <param name="query">被过滤的query</param>
    /// <param name="condition">过滤条件</param>
    /// <param name="predicate">过滤表达式</param>
    /// <returns>过滤后的结果</returns>
    public static TQueryable WhereIf<T, TQueryable>(this TQueryable query, bool condition,
        Expression<Func<T, bool>>? predicate)
        where TQueryable : IQueryable<T>
    {
        ThrowHelper.ThrowIfNull(query, nameof(query));

        if (predicate == null) return query;

        return condition
            ? (TQueryable)query.Where(predicate)
            : query;
    }
    
    /// <summary>
    /// 分页<see cref="IQueryable{T}"/>
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static IQueryable<TEntity> PageBy<TEntity>(
        this IQueryable<TEntity> query,
        int pageIndex,
        int pageSize)
        where TEntity : class
    {
        pageIndex = pageIndex < 1 ? 1 : pageIndex;
        pageSize = pageSize < 1 ? 1 : pageSize;

        return query.Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// 根据<paramref name="fields"/>排序<see cref="IQueryable{T}"/>
    /// </summary>
    /// <param name="query"></param>
    /// <param name="fields"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static IQueryable<TEntity> OrderBy<TEntity>(
        this IQueryable<TEntity> query,
        Dictionary<string, bool>? fields)
        where TEntity : class
    {
        var num = 0;
        foreach (var field in fields ?? Enumerable.Empty<KeyValuePair<string, bool>>())
        {
            query = num != 0
                ? query.ThenBy(field.Key, field.Value)
                : query.OrderBy(field.Key, field.Value);
            ++num;
        }

        return query;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="field"></param>
    /// <param name="desc"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    /// <exception cref="FakeException"></exception>
    public static IQueryable<TEntity> OrderBy<TEntity>(
        this IQueryable<TEntity> query,
        string field,
        bool desc = false)
        where TEntity : class
    {
        // todo：缓存
        var propertyInfo = GetPropertyInfo(typeof(TEntity), field)
                           ?? throw new FakeException($"{typeof(TEntity).Name}中找不到字段：{field}");
        var orderExpression = GetOrderExpression(typeof(TEntity), propertyInfo);
        return (desc
            ? typeof(Queryable).GetMethods()
                .FirstOrDefault(m =>
                    m.Name == "OrderByDescending" && m.GetParameters().Length == 2)
                ?.MakeGenericMethod(typeof(TEntity), propertyInfo.PropertyType).Invoke(null, [
                    query,
                    orderExpression
                ]) as IQueryable<TEntity>
            : (IQueryable<TEntity>?)typeof(Queryable).GetMethods()
                .FirstOrDefault(m =>
                    m.Name == nameof(OrderBy) && m.GetParameters().Length == 2)
                ?.MakeGenericMethod(typeof(TEntity), propertyInfo.PropertyType).Invoke(null, [
                    query,
                    orderExpression
                ])) ?? query;
    }

    private static IQueryable<TEntity> ThenBy<TEntity>(
        this IQueryable<TEntity> query,
        string field,
        bool desc)
        where TEntity : class
    {
        PropertyInfo propertyInfo = GetPropertyInfo(typeof(TEntity), field)
                                    ?? throw new FakeException($"{typeof(TEntity).Name}中找不到字段：{field}");
        LambdaExpression orderExpression = GetOrderExpression(typeof(TEntity), propertyInfo);
        return (desc
            ? typeof(Queryable).GetMethods()
                .FirstOrDefault(m => m.Name == "ThenByDescending" && m.GetParameters().Length == 2)
                ?.MakeGenericMethod(typeof(TEntity), propertyInfo.PropertyType).Invoke(null, new object[]
                {
                    query,
                    orderExpression
                }) as IQueryable<TEntity>
            : (IQueryable<TEntity>?)typeof(Queryable).GetMethods()
                .FirstOrDefault(m => m.Name == nameof(ThenBy) && m.GetParameters().Length == 2)
                ?.MakeGenericMethod(typeof(TEntity), propertyInfo.PropertyType).Invoke(null, new object[]
                {
                    query,
                    orderExpression
                })) ?? query;
    }

    private static PropertyInfo? GetPropertyInfo(Type entityType, string field) =>
        entityType.GetProperties().FirstOrDefault(p =>
            p.Name.Equals(field, StringComparison.OrdinalIgnoreCase));

    private static LambdaExpression GetOrderExpression(
        Type entityType,
        PropertyInfo propertyInfo)
    {
        var parameterExpression = Expression.Parameter(entityType);
        return Expression.Lambda(Expression.PropertyOrField(parameterExpression, propertyInfo.Name),
            parameterExpression);
    }
}