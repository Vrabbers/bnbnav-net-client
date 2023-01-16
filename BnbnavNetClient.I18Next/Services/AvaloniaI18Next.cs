using I18Next.Net;
using I18Next.Net.Plugins;
using System.Globalization;

namespace BnbnavNetClient.I18Next.Services;
sealed class AvaloniaI18Next : IAvaloniaI18Next
{
    I18NextNet? _i18Next;

    internal AvaloniaI18Next() { }

    public string this[string key, object? arguments]
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

    void IsNotNull()
    {
        if (_i18Next is null)
            throw new InvalidOperationException("IAvaloniaI18Next.Initialize(...) must be called before translations are accessed.");
    }

    public void Initialize(JsonResourcesFileBackend backend)
    {
        _i18Next = new I18NextNet(backend, new DefaultTranslator(backend, new TraceLogger(), new CldrPluralResolver(), new DefaultInterpolator()), new CultureInfoLanguageDetector());
        _i18Next.UseDetectedLanguage();
        _i18Next.SetFallbackLanguages("en");
    }
}