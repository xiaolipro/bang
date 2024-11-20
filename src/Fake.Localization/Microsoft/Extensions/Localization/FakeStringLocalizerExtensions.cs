using System;
using System.Collections.Generic;
using System.Reflection;
using Fake;
using Fake.Localization;

namespace Microsoft.Extensions.Localization;

public static class FakeStringLocalizerExtensions
{
    public static readonly string LocalizerFieldName = "_localizer";

    public static IEnumerable<LocalizedString> GetAllStrings(this IStringLocalizer stringLocalizer,
        bool includeParentCultures)
    {
        var internalLocalizer = stringLocalizer.GetInternalLocalizer();

        if (internalLocalizer is IFakeStringLocalizer fakeStringLocalizer)
        {
            return GetAllStrings(fakeStringLocalizer, includeParentCultures);
        }

        return stringLocalizer.GetAllStrings(includeParentCultures);
    }

    /// <summary>
    /// 获取内部_localizer
    /// </summary>
    /// <param name="stringLocalizer"></param>
    /// <returns></returns>
    /// <exception cref="FakeException">找不到字段</exception>
    public static IStringLocalizer GetInternalLocalizer(this IStringLocalizer stringLocalizer)
    {
        ThrowHelper.ThrowIfNull(stringLocalizer, nameof(stringLocalizer));

        var localizerType = stringLocalizer.GetType();

        if (!localizerType.IsAssignableTo(typeof(StringLocalizer<>))) return stringLocalizer;

        var localizerField = localizerType.GetField(LocalizerFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        if (localizerField == null)
        {
            throw new FakeException(
                $"无法在{typeof(StringLocalizer<>).FullName}中找到字段{LocalizerFieldName}，可能是命名发生变更，请联系作者");
        }

        return (IStringLocalizer)localizerField.GetValue(stringLocalizer)!;
    }
}