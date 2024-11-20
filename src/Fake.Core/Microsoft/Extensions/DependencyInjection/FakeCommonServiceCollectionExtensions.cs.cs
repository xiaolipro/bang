using Fake;

namespace Microsoft.Extensions.DependencyInjection;

public static class FakeCommonServiceCollectionExtensions
{
    public static bool IsAdded<T>(this IServiceCollection services)
    {
        return services.IsAdded(typeof(T));
    }

    public static bool IsAdded(this IServiceCollection services, Type type)
    {
        return services.Any(d => d.ServiceType == type);
    }


    public static void EnsureAdded<T>(this IServiceCollection services) where T : class
    {
        if (!services.IsAdded<T>())
        {
            throw new FakeInitializationException("请先添加服务：" + typeof(T).AssemblyQualifiedName);
        }
    }

    /// <summary>
    /// 直接从IOC容器中获取实例，找不到就返回null。
    /// tips：只是简单的根据type在services中找实现，请尽可能的从ServiceProvider取而不是通过此方法
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? GetInstanceOrNull<T>(this IServiceCollection services) where T : class
    {
        var serviceDecription = services.FirstOrDefault(d => d.ServiceType == typeof(T));

        if (serviceDecription == null) return null;

        var instance = serviceDecription.IsKeyedService
            ? serviceDecription.KeyedImplementationInstance
            : serviceDecription.ImplementationInstance;

        return instance?.To<T>();
    }

    /// <summary>
    /// 直接从IOC容器中获取实例。
    /// tips：只是简单的根据type在services中找实现，请尽可能的使用ServiceProvider取而不是通过此方法
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">找不到服务实例</exception>
    public static T GetInstance<T>(this IServiceCollection services) where T : class
    {
        var service = services.GetInstanceOrNull<T>();
        if (service == null)
        {
            throw new InvalidOperationException($"找不到服务实例：{typeof(T).AssemblyQualifiedName}");
        }

        return service;
    }

    /// <summary>
    /// 返回一个Lazy服务，valueFactory由<see cref="FakeApplication"/>的<see cref="ServiceProvider"/>提供
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Lazy<T> GetLazyRequiredService<T>(this IServiceCollection services) where T : class
    {
        return new Lazy<T>(services.GetRequiredService<T>, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// 从<see cref="FakeApplication"/>的<see cref="ServiceProvider"/>中获取服务<see cref="T"/>
    /// </summary>
    /// <param name="services"></param>
    /// <exception cref="FakeException"><see cref="FakeApplication"/> InitializeApplication执行前，不能使用</exception>
    /// <returns></returns>
    public static T GetRequiredService<T>(this IServiceCollection services) where T : class
    {
        return services.GetInstance<IFakeApplication>().ServiceProvider.GetRequiredService<T>();
    }

    public static IServiceProvider BuildServiceProviderFromFactory(this IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services, nameof(services));

        var factoryInterfaceType = services.FirstOrDefault(x =>
            x.ImplementationInstance?.GetType().GetInterfaces()
                .Where(t => t.IsGenericType)
                .Select(t => t.GetGenericTypeDefinition())
                .Contains(typeof(IServiceProviderFactory<>)
                ) ?? false)?.ServiceType;

        if (factoryInterfaceType == null)
        {
            // 如果没有第三方IOC容器则使用默认的
            return services.BuildServiceProvider();
        }

        // 通过服务商工厂创建服务商
        var containerBuilderType = factoryInterfaceType.GenericTypeArguments[0];
        var serviceProvider = typeof(FakeCommonServiceCollectionExtensions)
            .GetMethods()
            .Single(m => m is { Name: nameof(BuildServiceProviderFromFactory), IsGenericMethod: true })
            .MakeGenericMethod(containerBuilderType)
            .Invoke(null, [services, null!]);

        return serviceProvider!.To<IServiceProvider>();
    }

    public static IServiceProvider BuildServiceProviderFromFactory<TContainerBuilder>(
        this ServiceCollection services, Action<TContainerBuilder>? containerBuildAction = null)
        where TContainerBuilder : notnull
    {
        ThrowHelper.ThrowIfNull(services, nameof(services));


        // 这里默认是拿第一个实现，也就是第一个工厂
        // 一般来说我们要不就是使用默认的service provider，即aspnetcore通过BuildServiceProvider new出来的那个
        // 要不就是用类似autofac这种第三方provider，一个项目一般不会同时出现两个第三方service provider
        var serviceProviderFactory = services.GetInstanceOrNull<IServiceProviderFactory<TContainerBuilder>>();
        if (serviceProviderFactory == null)
        {
            throw new FakeException($"找不到{typeof(IServiceProviderFactory<TContainerBuilder>).FullName}的实现");
        }

        var builder = serviceProviderFactory.CreateBuilder(services);
        containerBuildAction?.Invoke(builder);

        return serviceProviderFactory.CreateServiceProvider(builder);
    }

    public static object? GetKeyedService(this IServiceProvider provider, Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(provider, nameof(provider));

        if (provider is IKeyedServiceProvider keyedServiceProvider)
        {
            return keyedServiceProvider.GetKeyedService(serviceType, serviceKey);
        }

        throw new InvalidOperationException("This service provider doesn't support keyed services.");
    }
}