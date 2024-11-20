namespace Fake.Helpers;

/// <summary>
/// 随机辅助类，该类提供的所有方法结果均是随机产生
/// </summary>
public static class RandomHelper
{
    /// <summary>
    /// NOTE 这里我们不再使用Guid.NewGuid().GetHashCode()做seed，因为内置的seed生成器更高效
    /// </summary>
    private static readonly ThreadLocal<Random> LocalRandom = new(() => new Random());

    /// <summary>
    /// 下一个随机数，该方法是线程安全的
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns>返回一个int类型随机数</returns>
    public static int Next(int minValue = 0, int maxValue = Int32.MaxValue)
    {
        return LocalRandom.Value!.Next(minValue, maxValue);
    }
    
    /// <summary>
    /// 将集合重新洗牌
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items"></param>
    public static List<T> Shuffle<T>(IEnumerable<T> items)
    {
        var original = new List<T>(items);
        var res = new List<T>();

        while (original.Any())
        {
            var idx = Next(0, original.Count);
            res.Add(original[idx]);
            original.RemoveAt(idx);
        }

        return res;
    }

    /// <summary>
    /// 获取目标数组的随机切片
    /// </summary>
    /// <param name="array">目标数组</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T[] Slice<T>(params T[] array)
    {
        return Shuffle(array).GetRange(0, Next(maxValue:array.Length)).ToArray();
    }
}