using System.Globalization;
using BnbnavNetClient.Services.TextToSpeech;
using CoreFoundation;

namespace BnbnavNetClient.Mac.TextToSpeech;

public class MacTextToSpeechProvider : ITextToSpeechProvider
{
    public MacTextToSpeechProvider()
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException();
        
        NSApplication.CheckForIllegalCrossThreadCalls = false;
    }
    
    public async Task SpeakAsync(string text)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException();

        await Task.Run(() =>
        {
            DispatchQueue.MainQueue.DispatchSync(() =>
            {
                var synth = new NSSpeechSynthesizer(NSSpeechSynthesizer.AvailableVoices[0]);
                synth.StartSpeakingString(text);
            });
        });
    }

    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}