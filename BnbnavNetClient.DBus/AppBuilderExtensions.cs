using Avalonia;
using BnbnavNetClient.DBus.DBusBusAddresses;
using Tmds.DBus;

namespace BnbnavNetClient.DBus;

public static class AppBuilderExtensions
{
    public static AppBuilder UseDBus(this AppBuilder appBuilder, IBusAddress busAddress)
    {
        appBuilder.With(new BnbnavDBusServer(busAddress));
        return appBuilder;
    }
}