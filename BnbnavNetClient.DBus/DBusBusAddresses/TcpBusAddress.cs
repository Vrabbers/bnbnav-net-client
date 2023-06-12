namespace BnbnavNetClient.DBus.DBusBusAddresses;

public class TcpBusAddress : IBusAddress
{
    public TcpBusAddress(string host, UInt32 port)
    {
        Host = host;
        Port = port;
    }

    string Host { get; }
    UInt32 Port { get; }
    public string BusAddress => $"tcp:host={Host},port={Port}";
}