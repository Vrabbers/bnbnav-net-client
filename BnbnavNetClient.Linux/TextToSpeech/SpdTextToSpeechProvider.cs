using System.Diagnostics;
using System.Globalization;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient.Linux.TextToSpeech;

public class SpdTextToSpeechProvider : ITextToSpeechProvider
{
    public SpdTextToSpeechProvider()
    {
        this.SpeakAsync("This is a test!", CultureInfo.CurrentUICulture);
    }
    
    public async Task SpeakAsync(string text, CultureInfo culture)
    {
        using var process = new Process();
        process.StartInfo.FileName = "spd-say";
        process.StartInfo.Arguments = string.Join(" ", 
            "-l", culture.TwoLetterISOLanguageName,
            $"\"{text}\"");
        process.Start();
        await process.WaitForExitAsync();
    }
}