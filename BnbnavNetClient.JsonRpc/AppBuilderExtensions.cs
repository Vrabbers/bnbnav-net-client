using Avalonia;

namespace BnbnavNetClient.JsonRpc;

public static class AppBuilderExtensions
{
    public static AppBuilder UseJsonRpc(this AppBuilder appBuilder, string domainSocket)
    {
        appBuilder.With(new JsonRpcServer(domainSocket));
        return appBuilder;
    }   
}