using BnbnavNetClient.I18Next.Pseudo;
using I18Next.Net;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace BnbnavNetClient.I18Next;

public class Translator : DefaultTranslator
{
    public Translator(ITranslationBackend backend, ILogger logger, IPluralResolver pluralResolver, IInterpolator interpolator) : base(backend, logger, pluralResolver, interpolator)
    {
    }

    public Translator(ITranslationBackend backend) : base(backend)
    {
    }

    public Translator(ITranslationBackend backend, IInterpolator interpolator) : base(backend, interpolator)
    {
    }

    public override async Task<string> TranslateAsync(string language, string key, IDictionary<string, object> args, TranslationOptions options)
    {
        var str = await base.TranslateAsync(language, key, args, options);
        if (PostProcessors.FirstOrDefault(x => x is PseudoLocalizationPostProcessor) is PseudoLocalizationPostProcessor
            pl)
        {
            str = pl.ProcessResult(key, str, args, language, this);
        }
        return str;
    }
}