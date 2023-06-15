using System.Net.Sockets;
using StreamJsonRpc;

namespace BnbnavNetClient.JsonRpc;

public class JsonRpcServer
{
    readonly Socket _socket;
    public JsonRpcServer(string domainSocket)
    {
        File.Delete(domainSocket);
        
        _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _socket.Bind(new UnixDomainSocketEndPoint(domainSocket));

        _ = Listen(CancellationToken.None);
    }

    public async Task Listen(CancellationToken ct)
    {
        try
        {
            _socket.Listen();
            while (true)
            {
                var client = await _socket.AcceptAsync(ct);
                var session = new JsonRpcSession(client);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }
}