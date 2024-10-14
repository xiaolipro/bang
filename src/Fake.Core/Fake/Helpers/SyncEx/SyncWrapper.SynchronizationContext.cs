namespace Fake.Helpers.SyncEx;

public sealed partial class SyncWrapper
{
    private sealed class SyncWrapperSynchronizationContext(SyncWrapper wrapper):SynchronizationContext
    {
        public SyncWrapper Wrapper => wrapper;

        public override void Post(SendOrPostCallback d, object state)
        {
            wrapper.Enqueue(wrapper._taskFactory.StartNew(() => d(state)), true);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (SyncWrapper.Current == wrapper)
            {
                d(state);
            }
            else
            {
                var task = wrapper._taskFactory.StartNew(() => d(state));
                task.WaitAndUnwrapException();
            }
        }

        public override void OperationStarted()
        {
            wrapper.OperationStarted();
        }

        public override void OperationCompleted()
        {
            wrapper.OperationCompleted();
        }
        
        public override SynchronizationContext CreateCopy()
        {
            return new SyncWrapperSynchronizationContext(wrapper);
        }
        
        public override int GetHashCode()
        {
            return wrapper.GetHashCode();
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is not SyncWrapperSynchronizationContext other)
            {
                return false;
            }
            
            return wrapper == other.Wrapper;
        }
    }
}