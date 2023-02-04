using System;
using System.Globalization;
using Avalonia;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient.Desktop.TextToSpeech;
public static class AppBuilderExtensions
{
    public static AppBuilder UseTextToSpeech(this AppBuilder appBuilder)
    {
        ITextToSpeechProvider textToSpeechProvider;

        if (OperatingSystem.IsWindows()) textToSpeechProvider = new WindowsTextToSpeechProvider();
        else if (OperatingSystem.IsLinux()) textToSpeechProvider = new DummyTextToSpeechProvider();
        // else if (OperatingSystem.IsBrowser()) textToSpeechProvider = new DummyTextToSpeechProvider();
        // else if (OperatingSystem.IsIOS()) textToSpeechProvider = new DummyTextToSpeechProvider();
        // else if (OperatingSystem.IsAndroid()) textToSpeechProvider = new DummyTextToSpeechProvider();
        else textToSpeechProvider = new DummyTextToSpeechProvider();

        textToSpeechProvider.SpeakAsync("I am working!", CultureInfo.CurrentUICulture);

        appBuilder.With(textToSpeechProvider);
        return appBuilder;
    }
}
