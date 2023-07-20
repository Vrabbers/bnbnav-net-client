using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services.Updates;

public interface IUpdateService
{
    Task CheckForUpdatesAsync();

    bool IsUpdateAvailable { get; }

    string? ManualInterventionInstructions { get; }
}