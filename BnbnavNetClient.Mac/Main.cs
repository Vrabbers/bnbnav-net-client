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
        .LogToTrace()
        .UseReactiveUI()
        .UseI18NextLocalization()
        .With<ITextToSpeechProvider>(new MacTextToSpeechProvider())
        .UseSettings(new SettingsManagerJsonFile());
        
BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);