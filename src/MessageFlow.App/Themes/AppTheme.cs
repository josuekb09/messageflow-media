using System.Windows;

namespace MessageFlow.App.Themes;

/// <summary>
/// Swaps the operator-chrome color dictionary at runtime. Projection output
/// windows are left unchanged so congregation screens stay high-contrast.
/// </summary>
public static class AppTheme
{
    private static readonly Uri DarkThemeUri = new("pack://application:,,,/Themes/DarkTheme.xaml", UriKind.Absolute);
    private static readonly Uri LightThemeUri = new("pack://application:,,,/Themes/LightTheme.xaml", UriKind.Absolute);

    public static void Apply(bool isLight)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return;
        }

        var themeUri = isLight ? LightThemeUri : DarkThemeUri;
        var dictionaries = application.Resources.MergedDictionaries;
        for (var index = 0; index < dictionaries.Count; index++)
        {
            var source = dictionaries[index].Source?.OriginalString ?? string.Empty;
            if (source.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
                source.Contains("LightTheme.xaml", StringComparison.OrdinalIgnoreCase))
            {
                dictionaries[index] = new ResourceDictionary { Source = themeUri };
                return;
            }
        }

        dictionaries.Insert(0, new ResourceDictionary { Source = themeUri });
    }
}
