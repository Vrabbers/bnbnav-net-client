using Tmds.DBus;

namespace BnbnavNetClient.DBus.DBusInterfaces;

public class Bnbnav : IBnbnav
{
    public ObjectPath ObjectPath => "/com/vrabbers/Bnbnav";

    public Task<string> PingAsync()
    {
        return Task.FromResult("Hello World!");
    }
}