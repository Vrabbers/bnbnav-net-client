using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class DummyEditController : IEditController
{
    public PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs _)
    {
        // Dummy
        return PointerPressedFlags.None;
    }

    public void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        // Dummy        
    }

    public void PointerReleased(MapView mapView, PointerReleasedEventArgs args) {
        
    }

    public void Render(MapView mapView, DrawingContext context)
    {
        
    }
}