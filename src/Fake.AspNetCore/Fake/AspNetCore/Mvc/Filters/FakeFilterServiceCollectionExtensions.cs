using Fake.AspNetCore.ExceptionHandling;
using Fake.Modularity;
using Fake.UnitOfWork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace Fake.AspNetCore.Mvc.Filters;

public static class FakeFilterServiceCollectionExtensions
{
    public static IServiceCollection AddFakeFilter<TFilter>(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient) where TFilter : class, IFilterMetadata
    {
        services.TryAdd(new ServiceDescriptor(typeof(TFilter), typeof(TFilter), lifetime));
        services.Configure<MvcOptions>(options => { options.Filters.AddService<TFilter>(); });
        return services;
    }

    public static IServiceCollection AddFakeExceptionFilter(this IServiceCollection services,
        Action<FakeExceptionHandlingOptions>? action = null)
    {
        services.AddFakeExceptionNotifier();

        services.TryAddTransient<IException2ErrorInfoConverter, DefaultException2ErrorInfoConverter>();
        services.TryAddTransient<IHttpExceptionStatusCodeFinder, DefaultHttpExceptionStatusCodeFinder>();

        services.AddFakeFilter<FakeExceptionFilter>();
        services.Configure<FakeExceptionHandlingOptions>(options =>
        {
            options.OutputStackTrace = services.GetInstanceOrNull<IWebHostEnvironment>()?.IsDevelopment() ?? false;
            action?.Invoke(options);
        });

        return services;
    }

    public static IServiceCollection AddFakeValidationActionFilter(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        return services.AddFakeFilter<FakeValidationActionFilter>();
    }

    public static IServiceCollection AddFakeUnitOfWorkActionFilter(this IServiceCollection services)
    {
        FakeModuleHelper.EnsureDependsOn<FakeUnitOfWorkModule>();
        return services.AddFakeFilter<FakeUnitOfWorkActionFilter>();
    }
}