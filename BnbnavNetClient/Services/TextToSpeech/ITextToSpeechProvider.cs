using System.Globalization;

namespace BnbnavNetClient.Services.TextToSpeech;

public interface ITextToSpeechProvider
{
    Task SpeakAsync(string text);
    
    CultureInfo CurrentCulture { get; set; }
}
