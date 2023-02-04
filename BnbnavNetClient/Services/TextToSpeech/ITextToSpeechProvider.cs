using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services.TextToSpeech;

public interface ITextToSpeechProvider
{
    Task SpeakAsync(string text);
    
    CultureInfo CurrentCulture { get; set; }
}
