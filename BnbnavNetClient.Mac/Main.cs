using Avalonia;
using Avalonia.ReactiveUI;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Mac.TextToSpeech;
using BnbnavNetClient.Services.TextToSpeech;
using BnbnavNetClient.Settings;

AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .UseReactiveUI()
        .UseI18NextLocalization()
        .UseTextToSpeechProvider(new MacTextToSpeechProvider())
        .UseSettings(new SettingsManagerJsonFile());
        
BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);