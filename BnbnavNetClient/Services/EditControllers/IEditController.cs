using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public enum PointerPressedFlags
{
    None = 0,
    DoNotPan = 1
}

public interface IEditController
{
    public PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args);
    public void PointerMoved(MapView mapView, PointerEventArgs args);
    public void PointerReleased(MapView mapView, PointerReleasedEventArgs args);
    public void Render(MapView mapView, DrawingContext context);
}