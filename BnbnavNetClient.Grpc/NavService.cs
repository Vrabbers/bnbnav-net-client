using Grpc.Core;

namespace BnbnavNetClient.Grpc;

public class NavService : Nav.NavBase
{
    public override Task<PingReply> Ping(PingMessage request, ServerCallContext context)
    {
        return Task.FromResult(new PingReply
        {
            Message = "Hello World!"
        });
    }
}