namespace BnbnavNetClient.Services.Updates;

public class DummyUpdateService : IUpdateService
{
    public bool IsUpdateAvailable => false;
    public string? ManualInterventionInstructions => null;

    public Task CheckForUpdatesAsync() => Task.CompletedTask; // no op

    public void RestartAppForUpdates() => throw new NotSupportedException();
}
