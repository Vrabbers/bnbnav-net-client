using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public interface INetworkOperation
{
    public Task PerformOperation();
    public void Render(MapView mapView, DrawingContext context);
}