namespace Fake.Application.Dtos;

/// <summary>
/// 分页结果
/// </summary>
/// <param name="totalCount"></param>
/// <param name="items"></param>
/// <typeparam name="T"></typeparam>
public class PagedResult<T>(int totalCount, IReadOnlyList<T> items):ListResult<T>(items)
{
    public int TotalCount { get; set; } = totalCount;

    public PagedResult() : this(0, [])
    {
    }
}