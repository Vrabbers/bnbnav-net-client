using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public abstract class NetworkOperation
{
    public abstract Task PerformOperation();
    public abstract void Render(MapView mapView, DrawingContext context);
    
    public List<MapItem> ItemsNotToRender { get; } = [];
}

public class NetworkOperationException(string message) : Exception(message);