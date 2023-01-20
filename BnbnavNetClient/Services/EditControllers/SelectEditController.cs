using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class SelectEditController : IEditController
{
    public PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        mapView.MapViewModel.Test = string.Empty;
        foreach (var hit in mapView.HitTest(pointerPos))
            mapView.MapViewModel.Test += hit.ToString() + "\n";

        return PointerPressedFlags.None;
    }

    public void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        
    }

    public void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
    }

    public void Render(MapView mapView, DrawingContext context)
    {
        
    }
}