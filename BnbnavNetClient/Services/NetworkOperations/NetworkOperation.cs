using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public abstract class NetworkOperation
{
    public abstract Task PerformOperation();
    public abstract void Render(MapView mapView, DrawingContext context);
    
    public List<MapItem> ItemsNotToRender { get; } = new();
}

public class NetworkOperationException : Exception
{
    public NetworkOperationException(string message) : base(message)
    {
        
    }
}