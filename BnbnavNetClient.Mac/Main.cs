using Avalonia;
using Avalonia.ReactiveUI;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.Mac.TextToSpeech;
using BnbnavNetClient.Services.TextToSpeech;
using BnbnavNetClient.Services.Updates;
using BnbnavNetClient.Settings;

AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .UseReactiveUI()
        .UseI18NextLocalization()
        .With<ITextToSpeechProvider>(new MacTextToSpeechProvider())
        .With<IUpdateService>(new DummyUpdateService())
        .UseSettings(new SettingsManagerJsonFile());
        
BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);