
using Avalonia.Markup.Xaml;

namespace BnbnavNetClient.i18n;

public class TString : MarkupExtension
{
    public string Name { get; set; }
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return $"ME STRING {Name}";
    }
}