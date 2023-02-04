using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace BnbnavNetClient.Settings;
public sealed class SettingsManagerJsonFile : ISettingsManager
{
    static string SettingsFilePath =>
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "bnbnav", "settings.json");

    public SettingsObject Settings { get; private set; } = null!;

    public async Task LoadAsync()
    {
        var file = new FileInfo(SettingsFilePath);
        if (!file.Directory!.Exists)
        {
            file.Directory.Create();
        }
        if (!file.Exists)
        {
            Settings = SettingsObject.Defaults;
            await SaveAsync();
            return;
        }

        await using var stream = File.Open(SettingsFilePath, FileMode.Open);
        var des = JsonSerializer.Deserialize<SettingsObject>(stream);
        if (des is null)
        {
            Settings = SettingsObject.Defaults;
            await SaveAsync();
            return;
        }
        Settings = des;
    }

    public async Task SaveAsync()
    {
        await using var stream = File.Create(SettingsFilePath);
        await JsonSerializer.SerializeAsync(stream, Settings);
    }
}
