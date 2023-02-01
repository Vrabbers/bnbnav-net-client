using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Web;
using Avalonia.ReactiveUI;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Settings;
using BnbnavNetClient.Services.TextToSpeech;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    private static void Main(string[] args) => BuildAvaloniaApp()
        .UseReactiveUI()
        .UseI18NextLocalization()
        .UseTextToSpeech()
        .UseSettings(new DummySettingsManager())
        .SetupBrowserApp("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}