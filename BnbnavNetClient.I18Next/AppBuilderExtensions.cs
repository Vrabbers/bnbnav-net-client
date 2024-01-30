using Avalonia;
using BnbnavNetClient.I18Next.Services;
using Splat;

namespace BnbnavNetClient.I18Next;
public static class AppBuilderExtensions
{
    public static AppBuilder UseI18NextLocalization(this AppBuilder appBuilder)
    {
        Locator.CurrentMutable.RegisterConstant<IAvaloniaI18Next>(new AvaloniaI18Next());
        return appBuilder;
    }
}
