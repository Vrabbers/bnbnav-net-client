using System.Net.Sockets;
using System.Text;
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
        var formatter = new JsonMessageFormatter(Encoding.UTF8);
        var handler = new NewLineDelimitedMessageHandler(_stream, _stream, formatter);
        var rpc = new StreamJsonRpc.JsonRpc(handler, _sessionObject);
        rpc.StartListening();
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