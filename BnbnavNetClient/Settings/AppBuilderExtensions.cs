using Avalonia;
using Splat;

namespace BnbnavNetClient.Settings;
public static class AppBuilderExtensions
{
    public static AppBuilder UseSettings(this AppBuilder appBuilder, ISettingsManager impl)
    {
        Locator.CurrentMutable.RegisterConstant(impl);
        return appBuilder;
    }
}
