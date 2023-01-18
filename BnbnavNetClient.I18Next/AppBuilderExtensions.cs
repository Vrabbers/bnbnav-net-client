using Avalonia;
using BnbnavNetClient.I18Next.Services;

namespace BnbnavNetClient.I18Next;
public static class AppBuilderExtensions
{
    public static AppBuilder UseI18NextLocalization(this AppBuilder appBuilder)
    {
        appBuilder.With<IAvaloniaI18Next>(new AvaloniaI18Next());
        return appBuilder;
    }
}
