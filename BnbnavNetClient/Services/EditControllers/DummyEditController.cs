using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class DummyEditController : EditController
{
    public override PointerPressed PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        // Dummy
        return EditControllers.PointerPressed.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        // Dummy        
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args) {
        
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        
    }
}