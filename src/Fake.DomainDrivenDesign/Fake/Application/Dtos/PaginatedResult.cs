namespace Fake.Application.Dtos;

/// <summary>
/// 分页结果
/// </summary>
/// <param name="totalCount"></param>
/// <param name="items"></param>
/// <typeparam name="T"></typeparam>
public class PaginatedResult<T>(long totalCount, IReadOnlyList<T> items):ListResult<T>(items)
{
    /*
     * 为什么total count是long，而不是int？
     * in fake，我们的实体倾向用程序生成的long类型自增id、或有序guid作为主键，而不是数据库自增int id。
     */
    
    /// <summary>
    /// 总条数
    /// </summary>
    public long TotalCount { get; set; } = totalCount;

    public PaginatedResult() : this(0, [])
    {
    }
}