using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Web;
using Avalonia.ReactiveUI;
using BnbnavNetClient;
using BnbnavNetClient.I18Next;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    private static void Main(string[] args) => BuildAvaloniaApp()
        .UseReactiveUI()
        .UseI18NextLocalization()
        .SetupBrowserApp("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}