using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Fake.Castle.DynamicProxy;

public class FakeMethodInvocationAdapter(
    IInvocation invocation,
    IInvocationProceedInfo proceedInfo,
    Func<IInvocation, IInvocationProceedInfo, Task> proceed)
    : FakeMethodInvocationAdapterBase(invocation)
{
    public override async Task ProcessAsync()
    {
        await proceed(Invocation, proceedInfo);
    }
}