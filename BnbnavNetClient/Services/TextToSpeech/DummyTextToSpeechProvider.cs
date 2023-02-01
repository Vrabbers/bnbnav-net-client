using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services.TextToSpeech;

public class DummyTextToSpeechProvider : ITextToSpeechProvider
{
    public Task SpeakAsync(string text, CultureInfo culture)
    {
        return Task.CompletedTask;
    }
}
