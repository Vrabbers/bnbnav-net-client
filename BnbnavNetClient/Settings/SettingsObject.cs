using System.Globalization;

namespace BnbnavNetClient.Settings;
public class SettingsObject
{
    public static SettingsObject Defaults => new();

    public string Language { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    public bool NightMode { get; set; }
    public string LoggedInUser { get; set; } = "";
}
