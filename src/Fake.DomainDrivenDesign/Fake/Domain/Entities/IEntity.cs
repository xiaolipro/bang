namespace Fake.Domain.Entities;

/// <summary>
/// 实体顶层抽象
/// 支持复合索引，单主键建议使用<see cref="IEntity{TKey}"/>
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 获取所有主键
    /// </summary>
    /// <returns></returns>
    object[] GetKeys();

    /// <summary>
    /// 是否为临时实体，表示还未持久化
    /// </summary>
    bool IsTransient();

    /// <summary>
    /// 实体相等性比较，默认根据keys比较
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    bool EntityEquals(IEntity? other);
}

/// <summary>
/// 实体顶层泛型抽象
/// </summary>
/// <typeparam name="TKey">唯一主键类型</typeparam>
public interface IEntity<out TKey> : IEntity where TKey : struct
{
    /// <summary>
    /// 实体唯一标识
    /// </summary>
    TKey Id { get; }
}