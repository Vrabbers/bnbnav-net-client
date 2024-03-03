using Avalonia;
using Avalonia.ReactiveUI;
using BnbnavNetClient.Windows.TextToSpeech;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Services.TextToSpeech;
using BnbnavNetClient.Settings;

namespace BnbnavNetClient.Windows;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .UseI18NextLocalization()
            .UseTextToSpeechProvider(new WindowsTextToSpeechProvider())
            .UseSettings(new SettingsManagerJsonFile());
}
