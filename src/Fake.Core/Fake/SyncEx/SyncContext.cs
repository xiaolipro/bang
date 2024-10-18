using System.Collections.Concurrent;

namespace Fake.SyncEx;

public sealed partial class SyncContext : IDisposable
{
    private readonly BlockingCollection<(Task task, bool propagateExceptions)> _queue;
    private readonly SyncContextTaskScheduler _taskScheduler;
    private readonly SyncContextSynchronizationContext _synchronizationContext;
    private readonly TaskFactory _taskFactory;
    private int _pendingNum;

    private static SyncContext? Current =>
        (SynchronizationContext.Current as SyncContextSynchronizationContext)?.Context;

    private SyncContext()
    {
        _queue = new BlockingCollection<(Task task, bool propagateExceptions)>();
        _taskScheduler = new SyncContextTaskScheduler(this);
        _synchronizationContext = new SyncContextSynchronizationContext(this);
        _taskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler,
            TaskContinuationOptions.HideScheduler | TaskContinuationOptions.DenyChildAttach, _taskScheduler);
    }

    private void Enqueue(Task task, bool propagateExceptions)
    {
        OperationStarted();
        task.ContinueWith(_ => OperationCompleted(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, _taskScheduler);
        _queue.TryAdd((task, propagateExceptions));
    }

    private void Execute()
    {
        var previousValue = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
        using (new DisposableWrapper(() => { SynchronizationContext.SetSynchronizationContext(previousValue); }))
        {
            var tasks = _queue.GetConsumingEnumerable();
            foreach (var (task, propagateExceptions) in tasks)
            {
                _taskScheduler.DoTryExecuteTask(task);

                if (propagateExceptions)
                {
                    task.WaitAndUnwrapException();
                }
            }
        }
    }

    private void OperationStarted()
    {
        Interlocked.Increment(ref _pendingNum);
    }

    /// <summary>
    /// 维护 <see cref="_pendingNum"/> 的值
    /// </summary>
    private void OperationCompleted()
    {
        var newCount = Interlocked.Decrement(ref _pendingNum);
        if (newCount == 0)
            _queue.CompleteAdding();
    }

    public static void Run(Func<Task> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        using var wrapper = new SyncContext();
        wrapper.OperationStarted();
        var task = wrapper._taskFactory.StartNew(func).ContinueWith(t =>
            {
                // ReSharper disable once AccessToDisposedClosure
                wrapper.OperationCompleted();
                return t.WaitAndUnwrapException();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, wrapper._taskScheduler);
        wrapper.Execute();
        task.WaitAndUnwrapException();
    }
    
    public static TResult Run<TResult>(Func<Task<TResult>> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        using var wrapper = new SyncContext();
        wrapper.OperationStarted();
        var task = wrapper._taskFactory.StartNew(func).Unwrap().ContinueWith(t =>
            {
                // ReSharper disable once AccessToDisposedClosure
                wrapper.OperationCompleted();
                return t.WaitAndUnwrapException();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, wrapper._taskScheduler);
        wrapper.Execute();
        return task.WaitAndUnwrapException();
    }

    public void Dispose()
    {
        _queue.Dispose();
    }
}