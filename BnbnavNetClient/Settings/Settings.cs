using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnbnavNetClient.Settings;
public class Settings
{
    public static Settings Defaults => new() { Language = "en", NightMode = false };

    public required string Language { get; set; }
    public required bool NightMode { get; set; }
}
