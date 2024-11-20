using System.Reflection;
using Autofac.Builder;
using Fake;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Autofac.Extensions.DependencyInjection;

public static class FakeRegistrationExtensions
{
    private const string AutofacServiceScopeFactoryClassName =
        "Autofac.Extensions.DependencyInjection.AutofacServiceScopeFactory";

    /// <summary>
    /// 将 <see cref="IServiceCollection"/> 中的服务填充到Autofac容器，
    /// 并使 <see cref="IServiceProvider"/> 和 <see cref="IServiceScopeFactory"/> 在Autofac容器中可用
    /// </summary>
    public static void Populate(
        this ContainerBuilder builder,
        IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services, nameof(services));

        builder.RegisterType<AutofacServiceProvider>()
            .As<IServiceProvider>()
            // hash结构，是的单个服务存在性验证从O(N) -> O(1)
            .As<IServiceProviderIsService>()
            // 使容器不释放实例
            .ExternallyOwned();

        // AutofacServiceScopeFactory可访问性是internal
        var factory = typeof(AutofacServiceProvider).Assembly.GetType(AutofacServiceScopeFactoryClassName);

        ThrowHelper.ThrowIfNull(factory, nameof(factory), $"找不到{AutofacServiceScopeFactoryClassName}！");

        // IServiceScopeFactory必须是单例的，平铺的，没有层级的
        builder.RegisterType(factory!)
            .As<IServiceScopeFactory>()
            .SingleInstance();

        Register(builder, services);
    }


    private static void Register(ContainerBuilder builder, IServiceCollection services)
    {
        var moduleContainer = services.GetInstance<IModuleContainer>();
        var registrationActionList = services.GetRegistrationActionList();

        foreach (var serviceDescriptor in services)
        {
            if (serviceDescriptor.ImplementationType != null)
            {
                var serviceTypeInfo = serviceDescriptor.ServiceType.GetTypeInfo();
                if (serviceTypeInfo.IsGenericTypeDefinition)
                {
                    builder.RegisterGeneric(serviceDescriptor.ImplementationType)
                        .As(serviceDescriptor.ServiceType)
                        .ConfigureLifecycle(serviceDescriptor.Lifetime)
                        .ConfigureFakeConventions(moduleContainer, registrationActionList);
                }
                else
                {
                    builder.RegisterType(serviceDescriptor.ImplementationType)
                        .As(serviceDescriptor.ServiceType)
                        .ConfigureLifecycle(serviceDescriptor.Lifetime)
                        .ConfigureFakeConventions(moduleContainer, registrationActionList);
                }
            }
            else if (serviceDescriptor.ImplementationFactory != null)
            {
                var registration = RegistrationBuilder.ForDelegate(serviceDescriptor.ServiceType,
                        (context, _) =>
                        {
                            var serviceProvider = context.Resolve<IServiceProvider>();
                            return serviceDescriptor.ImplementationFactory(serviceProvider);
                        })
                    .ConfigureLifecycle(serviceDescriptor.Lifetime)
                    .ConfigureFakeConventions(moduleContainer, registrationActionList)
                    .CreateRegistration();

                //TODO: registrationActionList ?
                builder.RegisterComponent(registration);
            }
            else
            {
                builder.RegisterInstance(serviceDescriptor.ImplementationInstance!)
                    .As(serviceDescriptor.ServiceType)
                    .ConfigureLifecycle(serviceDescriptor.Lifetime)
                    .ConfigureFakeConventions(moduleContainer, registrationActionList);
            }
        }
    }

    private static IRegistrationBuilder<object, TActivatorData, TRegistrationStyle> ConfigureLifecycle<TActivatorData,
        TRegistrationStyle>(
        this IRegistrationBuilder<object, TActivatorData, TRegistrationStyle> registrationBuilder,
        ServiceLifetime lifecycleKind)
    {
        switch (lifecycleKind)
        {
            case ServiceLifetime.Singleton:
                registrationBuilder.SingleInstance();
                break;
            case ServiceLifetime.Scoped:
                registrationBuilder.InstancePerLifetimeScope();
                break;
            case ServiceLifetime.Transient:
                registrationBuilder.InstancePerDependency();
                break;
        }

        return registrationBuilder;
    }
}