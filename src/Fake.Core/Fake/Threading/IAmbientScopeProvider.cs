namespace Fake.Threading;

/// <summary>
/// 环绕式作用域 供应商
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IAmbientScopeProvider<T>
{
    /// <summary>
    /// 获取当前所在作用域
    /// </summary>
    /// <param name="contextKey">上下文key</param>
    /// <returns></returns>
    T? GetValue(string contextKey);

    /// <summary>
    /// 开启一个新的作用域
    /// </summary>
    /// <param name="contextKey"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    IDisposable BeginScope(string contextKey, T value);
}