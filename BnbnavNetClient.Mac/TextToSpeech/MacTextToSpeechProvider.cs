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
                var filteredVoices = NSSpeechSynthesizer.AvailableVoices.Where(x =>
                {
                    var parts = x.Split(".");
                    return parts[^2].StartsWith(CurrentCulture.TwoLetterISOLanguageName);
                }).ToList();
                
                var voice = filteredVoices.Any() ? filteredVoices.First() : NSSpeechSynthesizer.AvailableVoices[0];
                var synth = new NSSpeechSynthesizer(voice);
                synth.StartSpeakingString(text);
            });
        });
    }

    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}