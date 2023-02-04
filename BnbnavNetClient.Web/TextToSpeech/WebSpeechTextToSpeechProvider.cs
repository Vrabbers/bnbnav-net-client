using System.Globalization;
using System.Threading.Tasks;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient.Web.TextToSpeech;

public class WebSpeechTextToSpeechProvider : ITextToSpeechProvider
{
    public Task SpeakAsync(string text, CultureInfo culture)
    {
        return Task.CompletedTask;
    }
}