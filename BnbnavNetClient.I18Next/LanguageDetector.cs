using System.Globalization;
using I18Next.Net.Plugins;

namespace BnbnavNetClient.I18Next;

public sealed class CultureInfoLanguageDetector : ILanguageDetector
{
    public string GetLanguage()
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    }
}