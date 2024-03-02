using Avalonia;
using Splat;

namespace BnbnavNetClient.Services.TextToSpeech;

public static class AppBuilderExtensions
{
    public static AppBuilder UseTextToSpeechProvider(this AppBuilder appBuilder, ITextToSpeechProvider tts)
    {
        Locator.CurrentMutable.RegisterConstant(tts);
        return appBuilder;
    }
}