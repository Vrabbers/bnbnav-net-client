using System.Threading.Tasks;
using BnbnavNetClient.Settings;

namespace BnbnavNetClient.iOS;

public class SettingsManagerI : ISettingsManager
{
    public SettingsObject Settings { get; } = SettingsObject.Defaults;

    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    public Task SaveAsync()
    {
        return Task.CompletedTask;
    }
}