using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Web;
using Avalonia.ReactiveUI;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Settings;
using BnbnavNetClient.Web.TextToSpeech;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    static void Main(string[] args) => BuildAvaloniaApp()
        .UseReactiveUI()
        .UseI18NextLocalization()
        .With(new WebSpeechTextToSpeechProvider())
        .UseSettings(new DummySettingsManager())
        .SetupBrowserApp("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}