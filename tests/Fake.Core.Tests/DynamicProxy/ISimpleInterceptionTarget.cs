using Fake.Castle.DynamicProxy;
using Fake.DependencyInjection;
using Fake.Logging;
using Fake.UnitOfWork;

namespace Fake.Core.Tests.DynamicProxy;

public interface ISimpleInterceptionTarget
{
    public List<string> Logs { get; }
    public Task DoItAsync();
}

public interface ICloseGenericInterceptionTarget : IGenericInterceptionTarget<string>
{
    
}

public class CloseGenericInterceptionTarget : GenericInterceptionTarget<string>, ICloseGenericInterceptionTarget, ITransientDependency
{
    
}

public interface IGenericInterceptionTarget<T>
{
    public List<string> Logs { get; }
    public Task DoItAsync();
}

[FakeIntercept(typeof(SimpleAsyncInterceptor))]
public class GenericInterceptionTarget<T> : ICanLog, IGenericInterceptionTarget<T>
{
    public List<string> Logs { get; } = new();

    public async Task DoItAsync()
    {
        Logs.Add("EnterDoItAsync");
        await Task.Delay(5);
        Logs.Add("MiddleDoItAsync");
        await Task.Delay(5);
        Logs.Add("ExitDoItAsync");
    }
}