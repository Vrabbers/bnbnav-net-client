namespace BnbnavNetClient.JsonRpc;

public class JsonRpcSessionObject
{
    public Task<string> PingAsync()
    {
        return Task.FromResult("Hello World!");
    }
}