namespace System.Threading;

public static class FakeTaskExtensions
{
    /// <summary>
    /// 等待任务完成并解包异常
    /// </summary>
    /// <param name="task"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void WaitAndUnwrapException(this Task task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));
        task.GetAwaiter().GetResult();
    }
    
    public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        return task.GetAwaiter().GetResult();
    }
}