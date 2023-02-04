using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services.TextToSpeech;

public class DummyTextToSpeechProvider : ITextToSpeechProvider
{
    public Task SpeakAsync(string text)
    {
        return Task.CompletedTask;
    }

    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}
