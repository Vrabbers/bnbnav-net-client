namespace BnbnavNetClient.JsonRpc;

public class BnbnavJsonRpcSession
{
    public Task<string> PingAsync()
    {
        return Task.FromResult("Hello World!");
    }
}