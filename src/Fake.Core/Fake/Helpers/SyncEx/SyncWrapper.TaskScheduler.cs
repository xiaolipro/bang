namespace Fake.Helpers.SyncEx;

public sealed partial class SyncWrapper
{
    private sealed class SyncWrapperTaskScheduler(SyncWrapper context) : TaskScheduler
    {
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return context._queue.Select(item => item.task);
        }

        protected override void QueueTask(Task task)
        {
            context.Enqueue(task, false);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return SyncWrapper.Current == context && TryExecuteTask(task);
        }


        public override int MaximumConcurrencyLevel => 1;


        /// <summary>
        /// 暴露 <see cref="TaskScheduler.TryExecuteTask"/> 方法
        /// </summary>
        /// <param name="task"></param>
        public void DoTryExecuteTask(Task task)
        {
            TryExecuteTask(task);
        }
    }
}