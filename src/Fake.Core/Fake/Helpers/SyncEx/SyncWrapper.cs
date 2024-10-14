using System.Collections.Concurrent;

namespace Fake.Helpers.SyncEx;

public sealed partial class SyncWrapper : IDisposable
{
    private readonly BlockingCollection<(Task task, bool propagateExceptions)> _queue;
    private readonly SyncWrapperTaskScheduler _taskScheduler;
    private readonly SyncWrapperSynchronizationContext _synchronizationContext;
    private readonly TaskFactory _taskFactory;
    private int _pendingNum;

    public static SyncWrapper? Current =>
        (SynchronizationContext.Current as SyncWrapperSynchronizationContext)?.Wrapper;

    public SyncWrapper()
    {
        _queue = new BlockingCollection<(Task task, bool propagateExceptions)>();
        _taskScheduler = new SyncWrapperTaskScheduler(this);
        _synchronizationContext = new SyncWrapperSynchronizationContext(this);
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
    /// Decrements the outstanding asynchronous operation count.
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

        using var wrapper = new SyncWrapper();
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

        using var wrapper = new SyncWrapper();
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