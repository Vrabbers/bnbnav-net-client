using System.Threading.Tasks;

namespace BnbnavNetClient.Settings;
public sealed class DummySettingsManager : ISettingsManager
{
    public SettingsObject Settings => SettingsObject.Defaults;

    public Task LoadAsync() => Task.CompletedTask;
    public Task SaveAsync() => Task.CompletedTask;
}
