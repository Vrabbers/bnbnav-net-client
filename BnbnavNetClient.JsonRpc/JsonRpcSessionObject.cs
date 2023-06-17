using Avalonia;
using BnbnavNetClient.Services;
using StreamJsonRpc;

namespace BnbnavNetClient.JsonRpc;

public class JsonRpcSessionObject
{
    readonly MapServiceProxy _mapServiceProxy;
    MapService? _mapService;
    
    public JsonRpcSessionObject()
    {
        _mapServiceProxy = AvaloniaLocator.Current.GetService<MapServiceProxy>()!;
        _mapServiceProxy.MapServiceChanged += OnMapServiceChanged;
        OnMapServiceChanged(this, EventArgs.Empty);
    }

    void OnMapServiceChanged(object? sender, EventArgs e)
    {
        _mapService = _mapServiceProxy.MapService;
    }

    public Task<string> PingAsync()
    {
        return Task.FromResult("Hello World!");
    }

    public Task<string> GetLoggedInUsernameAsync()
    {
        if (_mapService is null) throw new InvalidOperationException();
        if (_mapService.LoggedInUsername is null)
            throw new LocalRpcException("No logged in user")
            {
                ErrorCode = JsonRpcError.NoLoggedInUser
            };

        return Task.FromResult(_mapService.LoggedInUsername);
    }
}