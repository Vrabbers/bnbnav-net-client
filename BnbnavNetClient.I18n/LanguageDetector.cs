using System.Globalization;
using I18Next.Net.Plugins;

namespace BnbnavNetClient.i18n;

public class CultureInfoLanguageDetector : ILanguageDetector
{
    public string GetLanguage()
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    }
}