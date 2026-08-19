using System.Windows.Data;
using System.Windows.Markup;
using MessageFlow.Core.Localization;

namespace MessageFlow.App.Localization;

/// <summary>
/// XAML shorthand for a localized string: <c>Text="{loc:Tr Nav_Library}"</c>.
///
/// It returns a one-way binding to the Localizer indexer rather than a plain string, so
/// every localized element updates in place when the language changes. No restart, and
/// no language branching in XAML or code-behind.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension : MarkupExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
