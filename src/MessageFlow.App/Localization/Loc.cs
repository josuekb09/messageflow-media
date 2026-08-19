using MessageFlow.Core.Localization;

namespace MessageFlow.App.Localization;

internal static class Loc
{
    public static string T(string key)
    {
        return Localizer.Instance.Get(key);
    }

    public static string F(string key, params object?[] arguments)
    {
        return Localizer.Instance.Format(key, arguments);
    }

    public static string Count(int count, string singularKey, string pluralKey)
    {
        return Localizer.Instance.Count(count, singularKey, pluralKey);
    }
}
