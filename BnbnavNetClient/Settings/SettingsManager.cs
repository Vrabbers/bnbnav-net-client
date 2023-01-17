using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BnbnavNetClient.Settings;
internal static class SettingsManager
{
    static readonly string SettingsFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "bnbnav", "settings.json");
    public static Settings Settings { get; private set; } = null!;
    public static async Task LoadFromJsonAsync()
    {
        var file = new FileInfo(SettingsFilePath);
        if (!file.Directory!.Exists)
        {
            file.Directory.Create();
        }
        if (!file.Exists)
        {
            Settings = Settings.Defaults;
            await SaveAsync();
            return;
        }
        using var stream = File.Open(SettingsFilePath, FileMode.Open);
        var des = JsonSerializer.Deserialize<Settings>(stream);
        if (des is null)
        {
            Settings = Settings.Defaults;
            await SaveAsync();
            return;
        }
        Settings = des;
    }

    public static async Task SaveAsync()
    {
        using var stream = File.Create(SettingsFilePath);
        await JsonSerializer.SerializeAsync(stream, Settings);
        return;
    }
}
