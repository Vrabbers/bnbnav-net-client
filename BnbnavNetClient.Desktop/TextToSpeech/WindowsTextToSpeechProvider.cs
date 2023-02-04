﻿using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using BnbnavNetClient.Services.TextToSpeech;

namespace BnbnavNetClient.Desktop.TextToSpeech;
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

    public Task SpeakAsync(string text, CultureInfo culture)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        var prompt = new PromptBuilder(culture);
        prompt.AppendText(text);
        _speechSynthesizer.SpeakAsync(prompt);

        return Task.CompletedTask;
    }
}