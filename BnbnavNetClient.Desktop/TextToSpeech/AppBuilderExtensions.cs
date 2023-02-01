using System;
using Avalonia;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient.Desktop.TextToSpeech;
public static class AppBuilderExtensions
{
    public static AppBuilder UseTextToSpeech(this AppBuilder appBuilder)
    {
        ITextToSpeechProvider textToSpeechProvider;

        if (OperatingSystem.IsWindows()) textToSpeechProvider = new WindowsTextToSpeechProvider();
        else if (OperatingSystem.IsMacOS()) textToSpeechProvider = new DummyTextToSpeechProvider();
        else if (OperatingSystem.IsLinux()) textToSpeechProvider = new DummyTextToSpeechProvider();
        // else if (OperatingSystem.IsBrowser()) textToSpeechProvider = new DummyTextToSpeechProvider();
        // else if (OperatingSystem.IsIOS()) textToSpeechProvider = new DummyTextToSpeechProvider();
        // else if (OperatingSystem.IsAndroid()) textToSpeechProvider = new DummyTextToSpeechProvider();
        else textToSpeechProvider = new DummyTextToSpeechProvider();

        appBuilder.With(textToSpeechProvider);
        return appBuilder;
    }
}
