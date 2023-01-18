using Avalonia;

namespace BnbnavNetClient.Settings;
public static class AppBuilderExtensions
{
    public static AppBuilder UseSettings(this AppBuilder appBuilder, ISettingsManager impl)
    {
        appBuilder.With(impl);
        return appBuilder;
    }
}
