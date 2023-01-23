using System.Text;
using BnbnavNetClient.I18Next.Pseudo;
using I18Next.Net.Plugins;

namespace BnbnavNetClient.I18Next;

public class PseudoLocalizationPostProcessor : IPostProcessor
{
    private readonly PseudoLocalizationOptions _options;

    public string Process(string key, string value, IDictionary<string, object> args)
    {
        return ProcessResult(key, value, args, "", null);
    }

    public string Keyword => "pseudo";

    public PseudoLocalizationOptions Options => _options;

    public PseudoLocalizationPostProcessor(PseudoLocalizationOptions options)
    {
        _options = options;
    }

    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

    public string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator? translator)
    {
        var output = new StringBuilder();
        
        foreach (var c in value)
        {
            var newChar = _options.Letters.TryGetValue(c, out var c2) ? c2 : c;

            if (_options.RepeatedLetters.Contains(c))
                output.Append(Enumerable.Repeat(newChar, _options.LetterMultiplier).ToArray());
            else
                output.Append(newChar);
        }

        return output.ToString();
    }
}