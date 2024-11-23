namespace Fake.Timing;

public interface IFakeClock
{
    /// <summary>
    /// 当前时间
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// 时间种类
    /// </summary>
    DateTimeKind Kind { get; }

    /// <summary>
    /// 标准化
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    DateTime Normalize(DateTime dateTime);

    /// <summary>
    /// 标准化成string
    /// </summary>
    /// <param name="datetime"></param>
    /// <returns></returns>
    string NormalizeAsString(DateTime datetime);

    /// <summary>
    /// 度量动作执行时间
    /// </summary>
    /// <param name="action">需要评估的动作</param>
    /// <returns>动作所花费的时间</returns>
    TimeSpan MeasureExecutionTime(Action action);

    /// <summary>
    /// 度量异步任务执行时间
    /// </summary>
    /// <param name="task">需要评估的任务</param>
    /// <returns>任务所花费的时间</returns>
    Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> task);
}