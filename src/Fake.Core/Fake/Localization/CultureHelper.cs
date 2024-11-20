using System.Globalization;

namespace Fake.Localization;

public static class CultureHelper
{
    /// <summary>
    /// 在作用域销毁前，改变当前线程的文化，使用给定文化
    /// </summary>
    /// <param name="culture">给定文化</param>
    /// <param name="uiCulture">给定ui文化</param>
    /// <returns>可销毁的作用域</returns>
    public static IDisposable UseCulture(string culture, string? uiCulture = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(culture, nameof(culture));

        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = new CultureInfo(culture);
        CultureInfo.CurrentUICulture = new CultureInfo(uiCulture ?? culture);

        return new DisposableWrapper(() =>
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        });
    }


    /// <summary>
    /// 获取文化的父级文化名称
    /// </summary>
    /// <param name="cultureName"></param>
    /// <returns></returns>
    public static string GetParentCultureName(string cultureName)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(cultureName, nameof(cultureName));

        return new CultureInfo(cultureName).Parent.Name;
    }
}