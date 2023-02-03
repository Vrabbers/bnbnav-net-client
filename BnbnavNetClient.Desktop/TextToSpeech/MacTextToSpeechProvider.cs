using System;
using System.Globalization;
using System.Threading.Tasks;
using BnbnavNetClient.Services.TextToSpeech;

#if MACOS
using CoreFoundation;
using AppKit;
#endif

namespace BnbnavNetClient.Desktop.TextToSpeech;

public class MacTextToSpeechProvider : ITextToSpeechProvider
{
    public MacTextToSpeechProvider()
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException();
        
#if MACOS
        NSApplication.CheckForIllegalCrossThreadCalls = false;
#endif
    }
    
    public async Task SpeakAsync(string text, CultureInfo culture)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException();

#if MACOS
        await Task.Run(() =>
        {
            DispatchQueue.MainQueue.DispatchSync(() =>
            {
                var synth = new NSSpeechSynthesizer(NSSpeechSynthesizer.AvailableVoices[0]);
                synth.StartSpeakingString(text);
            });
        });
#endif
    }
}