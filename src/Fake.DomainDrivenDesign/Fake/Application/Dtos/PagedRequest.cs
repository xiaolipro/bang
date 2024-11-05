namespace Fake.Application.Dtos;

/// <summary>
/// 分页请求
/// </summary>
public class PagedRequest
{
    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>ss
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public Dictionary<string, bool>? SortFields { get; set; }
}