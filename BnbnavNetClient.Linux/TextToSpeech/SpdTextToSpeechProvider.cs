using System.Diagnostics;
using System.Globalization;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient.Linux.TextToSpeech;

public class SpdTextToSpeechProvider : ITextToSpeechProvider
{
    public async Task SpeakAsync(string text)
    {
        using var process = new Process();
        process.StartInfo.FileName = "spd-say";
        process.StartInfo.Arguments = string.Join(" ", 
            "-l", CurrentCulture.TwoLetterISOLanguageName,
            $"\"{text}\"");
        process.Start();
        await process.WaitForExitAsync();
    }

    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}