namespace BnbnavNetClient.Settings;
public interface ISettingsManager
{
    SettingsObject Settings { get; }

    Task LoadAsync();
    Task SaveAsync();
}
