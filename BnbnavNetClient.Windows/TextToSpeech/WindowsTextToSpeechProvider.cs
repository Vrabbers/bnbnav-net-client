using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient.Windows.TextToSpeech;
class WindowsTextToSpeechProvider : ITextToSpeechProvider
{
    SpeechSynthesizer _speechSynthesizer;

    public WindowsTextToSpeechProvider()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        _speechSynthesizer = new SpeechSynthesizer();
        _speechSynthesizer.SetOutputToDefaultAudioDevice();
    }

    public Task SpeakAsync(string text)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        var prompt = new PromptBuilder(CurrentCulture);
        prompt.AppendText(text);
        _speechSynthesizer.SpeakAsync(prompt);

        return Task.CompletedTask;
    }

    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}
