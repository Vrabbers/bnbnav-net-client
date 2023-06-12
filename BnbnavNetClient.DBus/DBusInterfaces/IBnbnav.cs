using Tmds.DBus;

namespace BnbnavNetClient.DBus.DBusInterfaces;

[DBusInterface("com.vrabbers.Bnbnav")]
public interface IBnbnav : IDBusObject
{
    public Task<string> PingAsync();
}