using Avalonia;
using Avalonia.ReactiveUI;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Linux.TextToSpeech;
using BnbnavNetClient.Services.TextToSpeech;
using BnbnavNetClient.Settings;

namespace BnbnavNetClient.Linux;

internal static class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .UseI18NextLocalization()
            .UseTextToSpeechProvider(new SpdTextToSpeechProvider())
            .UseSettings(new SettingsManagerJsonFile());
    }
}