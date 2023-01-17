using System.Globalization;
using I18Next.Net.Plugins;

namespace BnbnavNetClient.I18Next;

public sealed class CultureInfoLanguageDetector : ILanguageDetector
{
    public string GetLanguage()
    {
        return CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        // TODO: change to .Name so it can get regional variants?
    }
}