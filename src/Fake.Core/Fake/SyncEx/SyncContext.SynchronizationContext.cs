namespace Fake.SyncEx;

public sealed partial class SyncContext
{
    private sealed class SyncContextSynchronizationContext(SyncContext context):SynchronizationContext
    {
        public SyncContext Context => context;

        public override void Post(SendOrPostCallback d, object state)
        {
            context.Enqueue(context._taskFactory.StartNew(() => d(state)), true);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (SyncContext.Current == context)
            {
                d(state);
            }
            else
            {
                var task = context._taskFactory.StartNew(() => d(state));
                task.WaitAndUnwrapException();
            }
        }

        public override void OperationStarted()
        {
            context.OperationStarted();
        }

        public override void OperationCompleted()
        {
            context.OperationCompleted();
        }
        
        public override SynchronizationContext CreateCopy()
        {
            return new SyncContextSynchronizationContext(context);
        }
        
        public override int GetHashCode()
        {
            return context.GetHashCode();
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is not SyncContextSynchronizationContext other)
            {
                return false;
            }
            
            return context == other.Context;
        }
    }
}