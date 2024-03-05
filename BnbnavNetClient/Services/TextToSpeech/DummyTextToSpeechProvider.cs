using System.Globalization;

namespace BnbnavNetClient.Services.TextToSpeech;

public class DummyTextToSpeechProvider : ITextToSpeechProvider
{
    public Task SpeakAsync(string text)
    {
        return Task.CompletedTask;
    }

    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}
