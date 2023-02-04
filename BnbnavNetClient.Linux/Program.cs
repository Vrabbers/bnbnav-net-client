// See https://aka.ms/new-console-template for more information

using Avalonia;
using Avalonia.ReactiveUI;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Linux.TextToSpeech;
using BnbnavNetClient.Settings;

AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .UseReactiveUI()
        .UseI18NextLocalization()
        .With(new SpdTextToSpeechProvider())
        .UseSettings(new SettingsManagerJsonFile());
        
BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);