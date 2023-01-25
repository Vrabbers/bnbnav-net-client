using I18Next.Net;
using I18Next.Net.Plugins;
using System.Globalization;
using BnbnavNetClient.I18Next.Pseudo;

namespace BnbnavNetClient.I18Next.Services;
sealed class AvaloniaI18Next : IAvaloniaI18Next
{
    I18NextNet? _i18Next;

    public string this[string key, Dictionary<string, object?>? arguments]
    {
        get
        {
            IsNotNull();
            return _i18Next!.T(key, arguments);
        }
    }

    public Task<string> TAsync(string key, object? arguments)
    {
        IsNotNull();
        return _i18Next!.Ta(key, arguments);
    }

    public bool IsRightToLeft
    {
        get 
        {
            IsNotNull();
            return new CultureInfo(_i18Next!.Language).TextInfo.IsRightToLeft;
        }
    }

    public IEnumerable<CultureInfo> AvailableLanguages { get; private set; } = null!;
    public CultureInfo CurrentLanguage
    {
        get
        {
            return new(_i18Next!.Language);
        }

        set
        {
            CultureInfo.CurrentUICulture = value;
            CultureInfo.CurrentCulture = value;
            _i18Next!.UseDetectedLanguage();
        }
    }

    void IsNotNull()
    {
        if (_i18Next is null)
            throw new InvalidOperationException("IAvaloniaI18Next.Initialize(...) must be called before translations are accessed.");
    }

    public void Initialize(JsonResourcesFileBackend backend, bool pseudo)
    {
        var translator = new Translator(backend, new TraceLogger(), new CldrPluralResolver(),
            new DefaultInterpolator());
        if (pseudo)
        {
            translator.PostProcessors.Add(new PseudoLocalizationPostProcessor(new()));
        }

        _i18Next = new(backend, translator, new CultureInfoLanguageDetector());
        _i18Next.UseDetectedLanguage();
        _i18Next.SetFallbackLanguages("en");

        AvailableLanguages = backend.AvailableLanguages;
    }
}