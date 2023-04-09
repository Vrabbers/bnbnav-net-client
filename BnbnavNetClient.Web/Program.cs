using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Services.TextToSpeech;
using BnbnavNetClient.Settings;
using BnbnavNetClient.Web.TextToSpeech;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    static async Task Main() => await BuildAvaloniaApp()
        .With<ITextToSpeechProvider>(new WebSpeechTextToSpeechProvider())
        .UseReactiveUI()
        .UseI18NextLocalization()
        .UseSettings(new DummySettingsManager())
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}