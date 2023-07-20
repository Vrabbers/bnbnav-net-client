namespace BnbnavNetClient.Services.Updates;

public interface IUpdateService
{
    Task CheckForUpdatesAsync();

    bool IsUpdateAvailable { get; }

    string? ManualInterventionInstructions { get; }
}