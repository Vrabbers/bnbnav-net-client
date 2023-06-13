using Avalonia;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BnbnavNetClient.Grpc;

public static class AppBuilderExtensions
{
    public static AppBuilder UseGrpc(this AppBuilder appBuilder, string socketPath)
    {
        File.Delete(socketPath);
        
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddGrpc();
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenUnixSocket(socketPath, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        var app = builder.Build();
        app.MapGrpcService<NavService>();
        _ = app.RunAsync();
        
        return appBuilder;
    }
}