using System.Net.Sockets;
using StreamJsonRpc;

namespace BnbnavNetClient.JsonRpc;

public class JsonRpcSession
{
    readonly NetworkStream _stream;
    readonly JsonRpcSessionObject _sessionObject = new();
    
    public JsonRpcSession(Socket socket)
    {
        _stream = new NetworkStream(socket);

        _ = StartRpcAsync();
    }

    public async Task StartRpcAsync()
    {
        var rpc = StreamJsonRpc.JsonRpc.Attach(_stream, _sessionObject);
        try
        {
            await rpc.Completion;
        }
        catch (RemoteRpcException ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}