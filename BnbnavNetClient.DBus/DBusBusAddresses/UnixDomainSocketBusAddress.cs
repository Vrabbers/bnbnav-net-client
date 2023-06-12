namespace BnbnavNetClient.DBus.DBusBusAddresses;

public class UnixDomainSocketBusAddress : IBusAddress
{
    public UnixDomainSocketBusAddress(string path)
    {
        Path = path;
    }

    public string Path { get; }
    public string BusAddress => $"unix:path={Path}";
}