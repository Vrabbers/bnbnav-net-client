
using Avalonia;
using Avalonia.Markup.Xaml;
using BnbnavNetClient.I18Next.Services;

namespace BnbnavNetClient.I18Next;

public sealed class Tr : MarkupExtension
{
    public string Key { get; set; } = null!;
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>()[Key];
    }
}