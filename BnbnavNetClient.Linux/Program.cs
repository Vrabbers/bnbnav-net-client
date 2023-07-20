// See https://aka.ms/new-console-template for more information

using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Svg.Skia;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Linux.TextToSpeech;
using BnbnavNetClient.Services.TextToSpeech;
using BnbnavNetClient.Services.Updates;
using BnbnavNetClient.Settings;

namespace BnbnavNetClient.Linux;

internal class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    static AppBuilder BuildAvaloniaApp()
    {
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI()
            .UseI18NextLocalization()
            .With<ITextToSpeechProvider>(new SpdTextToSpeechProvider())
            .With<IUpdateService>(new DummyUpdateService())
            .UseSettings(new SettingsManagerJsonFile());
    }
}