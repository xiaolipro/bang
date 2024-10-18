namespace Fake.Application.Dtos;

/// <summary>
/// 列表结果
/// </summary>
/// <param name="items"></param>
/// <typeparam name="T"></typeparam>
public class ListResult<T>(IReadOnlyList<T> items)
{
    public IReadOnlyList<T> Items { get; set; } = items;
}