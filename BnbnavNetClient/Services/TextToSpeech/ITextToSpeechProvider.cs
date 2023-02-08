using System.Globalization;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services.TextToSpeech;

public interface ITextToSpeechProvider
{
    Task SpeakAsync(string text);
    
    CultureInfo CurrentCulture { get; set; }
}
