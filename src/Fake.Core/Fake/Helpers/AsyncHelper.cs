using Fake.Helpers.SyncEx;

namespace Fake.Helpers;

public static class AsyncHelper
{
    /// <summary>
    /// 同步运行任务
    /// </summary>
    /// <param name="func"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return SyncWrapper.Run(func);
    }

    public static void RunSync(Func<Task> func)
    {
        SyncWrapper.Run(func);
    }

    public static void RunSync(Task task)
    {
        SyncWrapper.Run(() => task);
    }
}