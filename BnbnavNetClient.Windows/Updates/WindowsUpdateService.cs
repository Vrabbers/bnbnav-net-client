using BnbnavNetClient.Services.Updates;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BnbnavNetClient.Windows.Updates;
class WindowsUpdateService : IUpdateService
{
    public bool IsUpdateAvailable { get; private set; } = false;
    public string? ManualInterventionInstructions => null;

    readonly string rootPath;
    const string updatePath = "https://github.com/Vrabbers/bnbnav-net-client";
    public WindowsUpdateService()
    {
        rootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, @"../");
    }

    public Task CheckForUpdatesAsync() => CheckForUpdatesInternalAsync();

    async Task CheckForUpdatesInternalAsync(bool useDeltaPatching = true)
    {
        if (!File.Exists(Path.Combine(rootPath, "Update.exe"))) return; // not an installed version of bnbnav; bail

        using var updateManager = new GithubUpdateManager(updatePath);
        try
        {
            var updateInfo = await updateManager.CheckForUpdate(!useDeltaPatching);
            if (updateInfo.CurrentlyInstalledVersion == null) return;
            if (updateInfo.ReleasesToApply.Count == 0)
            {
                IsUpdateAvailable = false;
                return;
            }

            await updateManager.DownloadReleases(updateInfo.ReleasesToApply);
            await updateManager.ApplyReleases(updateInfo);

            IsUpdateAvailable = true;
        }
        catch
        {
            if (useDeltaPatching)
            {
                // updating can fail if deltas are unavailable for full update path (https://github.com/Squirrel/Squirrel.Windows/issues/959)
                // so let's try again without deltas
                await CheckForUpdatesInternalAsync(false);
                return;
            }
            else throw;
        }
    }
}
