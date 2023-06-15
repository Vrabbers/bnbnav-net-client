using System.Net.Sockets;

namespace BnbnavNetClient.JsonRpc;

public class BnbnavJsonRpcServer
{
    readonly Socket _socket;
    public BnbnavJsonRpcServer(string domainSocket)
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
                var stream = new NetworkStream(client);
                var session = new BnbnavJsonRpcSession();
                var rpc = StreamJsonRpc.JsonRpc.Attach(stream, session);
                rpc.StartListening();
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }
}