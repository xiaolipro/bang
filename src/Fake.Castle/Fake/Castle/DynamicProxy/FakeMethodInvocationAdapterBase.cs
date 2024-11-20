using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Fake.DynamicProxy;

namespace Fake.Castle.DynamicProxy;

public abstract class FakeMethodInvocationAdapterBase : IFakeMethodInvocation
{
    public object[] Arguments => Invocation.Arguments;
    private readonly Lazy<IReadOnlyDictionary<string, object>> _lazyArgumentsDictionary;
    public IReadOnlyDictionary<string, object> ArgumentsDictionary => _lazyArgumentsDictionary.Value;
    public Type[] GenericArguments => Invocation.GenericArguments;
    public object TargetObject => Invocation.InvocationTarget ?? Invocation.MethodInvocationTarget;
    public MethodInfo Method => Invocation.MethodInvocationTarget ?? Invocation.Method;
    public object? ReturnValue { get; set; }

    public FakeMethodInvocationAdapterBase(IInvocation invocation)
    {
        Invocation = invocation;
        _lazyArgumentsDictionary = new Lazy<IReadOnlyDictionary<string, object>>(GetArgumentsDictionary);
    }

    protected IInvocation Invocation { get; }
    public abstract Task ProcessAsync();


    private IReadOnlyDictionary<string, object> GetArgumentsDictionary()
    {
        var dict = new Dictionary<string, object>();

        var methodParameters = Method.GetParameters();
        for (var i = 0; i < methodParameters.Length; i++)
        {
            dict[methodParameters[i].Name!] = Invocation.Arguments[i];
        }

        return dict;
    }
}