using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class SelectEditController : EditController
{
    public override PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        mapView.MapViewModel.Test = string.Empty;
        foreach (var hit in mapView.HitTest(pointerPos))
            mapView.MapViewModel.Test += hit.ToString() + "\n";

        return PointerPressedFlags.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        
    }
}