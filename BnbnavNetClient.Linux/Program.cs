// See https://aka.ms/new-console-template for more information

using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Svg.Skia;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;
using BnbnavNetClient.JsonRpc;
using BnbnavNetClient.Linux.TextToSpeech;
using BnbnavNetClient.Services.TextToSpeech;
using BnbnavNetClient.Settings;

GC.KeepAlive(typeof(SvgImageExtension).Assembly);
GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

return AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .LogToTrace()
    .UseReactiveUI()
    .UseJsonRpc($"{Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") ?? $"/tmp/runtime-{Environment.UserName}"}/bnbnav")
    .UseI18NextLocalization()
    .With<ITextToSpeechProvider>(new SpdTextToSpeechProvider())
    .UseSettings(new SettingsManagerJsonFile())
    
    .StartWithClassicDesktopLifetime(args);