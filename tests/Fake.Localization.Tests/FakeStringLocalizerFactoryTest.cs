using Fake.Localization.Tests.Localization;
using Fake.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Shouldly;
using Xunit;

namespace Fake.Localization.Tests;

public sealed class FakeStringLocalizerFactoryTest : ApplicationTest<FakeLocalizationTestModule>
{
    private readonly IStringLocalizerFactory _localizerFactory;

    public FakeStringLocalizerFactoryTest()
    {
        _localizerFactory = ServiceProvider.GetRequiredService<IStringLocalizerFactory>();
    }

    [Fact]
    public void 内部的StringLocalizer一定是InMemoryStringLocalizer()
    {
        var internalLocalizer = _localizerFactory.Create(typeof(LocalizationTestResource)).GetInternalLocalizer();
        internalLocalizer.ShouldNotBeNull();
        internalLocalizer.ShouldBeOfType<InMemoryStringLocalizer>();
    }

    [Fact]
    public void 可以实现本地化()
    {
        var localizer = _localizerFactory.Create(typeof(LocalizationTestResource));
        localizer["Hi"].Value.ShouldBe("你好");
    }

    [Fact]
    public void 没有给定资源类型的也能支持()
    {
        var localizer = _localizerFactory.CreateByResourceNameOrNull("LocalizationTestCountryNames");

        localizer.ShouldNotBeNull();
        localizer["USA"].Value.ShouldBe("美利坚");
    }
}