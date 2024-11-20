using Fake;

namespace System.Collections.Generic;

public static class FakeEnumerableExtensions
{
    /// <summary>
    /// 检查给定的集合对象是否没有元素。
    /// </summary>
    public static bool IsEmpty<T>(this IEnumerable<T> source)
    {
        return !(source ?? throw new ArgumentNullException(nameof(source))).Any();
    }

    /// <summary>
    /// 检查给定的集合对象是否为null或没有元素。
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source == null || !source.Any();
    }

    /// <summary>
    /// 提供索引
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        int counter = 0;
        foreach (var item in source)
        {
            yield return (item, index: counter++);
        }
    }

    /// <summary>
    /// 将集合用separator拼接，等同string.Join，但不会因为source is null而抛<see cref="ArgumentNullException"/>
    /// </summary>
    /// <param name="source"></param>
    /// <param name="separator"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>返回拼接后的字符串</returns>
    public static string JoinAsString<T>(this IEnumerable<T>? source, string separator)
    {
        if (source == null) return "";
        return string.Join(separator, source);
    }

    /// <summary>
    /// 根据元素间的依赖关系进行拓扑排序
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="source">待排序的集合，他们的入度为0</param>
    /// <param name="getDependencies">获取元素依赖关系的函数</param>
    /// <param name="comparer">节点相等性比较器</param>
    /// <returns>
    /// 返回一个按照依赖关系排序后的新的list，他是一个拓扑序列，
    /// 即满足如果A依赖B，则新的list中B会在A之前出现。
    /// </returns>
    /// <exception cref="FakeException">如果依赖图中存在环，则抛出异常</exception>
    public static List<T> SortByDependencies<T>(
        this IEnumerable<T> source,
        Func<T, IEnumerable<T>> getDependencies,
        IEqualityComparer<T>? comparer = null) where T : notnull
    {
        /* See: http://www.codeproject.com/Articles/869059/Topological-sorting-in-Csharp
         *      http://en.wikipedia.org/wiki/Topological_sorting
         */

        var sorted = new List<T>();

        // 维护一个访问记录hash
        var visited = new Dictionary<T, bool>(comparer);

        foreach (var item in source)
        {
            SortByDependenciesVisit(item, getDependencies, sorted, visited);
        }

        return sorted;
    }

    private static void SortByDependenciesVisit<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted,
        Dictionary<T, bool> visited) where T : notnull
    {
        var alreadyVisited = visited.TryGetValue(item, out var processing);

        if (alreadyVisited)
        {
            // 如果已经访问过了，并且还在递归栈中，则出现了循环引用
            if (processing)
            {
                throw new FakeException("发现循环依赖! 依赖项: " + item);
            }

            // 剪枝
            return;
        }

        // 标记访问
        visited[item] = true;

        // 递归处理以item为起点，连通的其它点
        var dependencies = getDependencies(item);
        foreach (var dependency in dependencies)
        {
            SortByDependenciesVisit(dependency, getDependencies, sorted, visited);
        }

        // 恢复现场
        visited[item] = false;
        // 此时item的出度为0
        sorted.Add(item);
    }
}