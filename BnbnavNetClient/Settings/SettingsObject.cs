using System.Globalization;

namespace BnbnavNetClient.Settings;
public class SettingsObject
{
    public static SettingsObject Defaults => new() 
    {
        Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
        NightMode = false
    };

    public required string Language { get; set; }
    public required bool NightMode { get; set; }
}
