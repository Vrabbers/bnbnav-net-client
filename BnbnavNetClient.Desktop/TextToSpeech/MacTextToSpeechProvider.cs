using System;
using System.Globalization;
using System.Threading.Tasks;
using BnbnavNetClient.Services.TextToSpeech;

#if MACOS
using AppKit;
#endif

namespace BnbnavNetClient.Desktop.TextToSpeech;

public class MacTextToSpeechProvider : ITextToSpeechProvider
{
#if MACOS
    readonly NSSpeechSynthesizer _synth;
#endif

    public MacTextToSpeechProvider()
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException();
#if MACOS
        _synth = new NSSpeechSynthesizer(NSSpeechSynthesizer.AvailableVoices[0]);
#endif
    }
    
    public async Task SpeakAsync(string text, CultureInfo culture)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException();

#if MACOS
        _synth.StartSpeakingString(text);
#endif
    }
}