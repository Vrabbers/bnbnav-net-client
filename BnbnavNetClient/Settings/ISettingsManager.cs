using System.Threading.Tasks;

namespace BnbnavNetClient.Settings;
public interface ISettingsManager
{
    SettingsObject Settings { get; }

    Task LoadAsync();
    Task SaveAsync();
}
