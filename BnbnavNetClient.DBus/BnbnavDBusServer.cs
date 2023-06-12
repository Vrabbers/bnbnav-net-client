using BnbnavNetClient.DBus.DBusBusAddresses;
using BnbnavNetClient.DBus.DBusInterfaces;
using Tmds.DBus;

namespace BnbnavNetClient.DBus;

public class BnbnavDBusServer
{
    readonly IBusAddress _busAddress;
    readonly ServerConnectionOptions _server;
    readonly Connection _connection;
    
    public BnbnavDBusServer(IBusAddress busAddress)
    {
        _busAddress = busAddress;
        _server = new ServerConnectionOptions();
        _connection = new Connection(_server);
        
        _ = SetupDBusServer();
    }

    async Task SetupDBusServer()
    {
        if (_busAddress is UnixDomainSocketBusAddress uds)
        {
            File.Delete(uds.Path);
        }
        
        await _connection.RegisterObjectAsync(new Bnbnav());
        await _server.StartAsync(_busAddress.BusAddress);
    }
}