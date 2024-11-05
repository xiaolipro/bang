using Fake.Modularity;
using Fake.VirtualFileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace Fake.Localization;

[DependsOn(
    typeof(FakeVirtualFileSystemModule)
)]
public class FakeLocalizationModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 替换ASPNETCORE原生的IStringLocalizerFactory实现
        context.Services.Replace(ServiceDescriptor.Singleton<IStringLocalizerFactory, FakeStringLocalizerFactory>());
        context.Services.AddSingleton<IFakeStringLocalizerFactory, FakeStringLocalizerFactory>();
        context.Services.AddSingleton<ResourceManagerStringLocalizerFactory>();

        context.Services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        context.Services.TryAddTransient<IStringLocalizer, InMemoryStringLocalizer>();

        context.Services.Configure<FakeVirtualFileSystemOptions>(options =>
        {
            options.FileProviders.Add<FakeLocalizationModule>("Fake.Localization");
        });

        context.Services.Configure<FakeLocalizationOptions>(options =>
        {
            options.Resources.Add<FakeLocalizationResource>("zh")
                .LoadVirtualJson("/Resources");
        });
    }
}