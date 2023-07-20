namespace BnbnavNetClient.Services.Updates;

public class DummyUpdateProvider : IUpdateService
{
    public bool IsUpdateAvailable => false;
    public string? ManualInterventionInstructions => null;

    public Task CheckForUpdatesAsync() => Task.CompletedTask; // no op
}
