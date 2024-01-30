using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public enum PointerPressed
{
    None = 0,
    DoNotPan = 1
}

public abstract class EditController
{
    public abstract PointerPressed PointerPressed(MapView mapView, PointerPressedEventArgs args);
    public abstract void PointerMoved(MapView mapView, PointerEventArgs args);
    public abstract void PointerReleased(MapView mapView, PointerReleasedEventArgs args);
    public abstract void Render(MapView mapView, DrawingContext context);

    public IList<MapItem> ItemsNotToRender { get; set; } = new List<MapItem>();
}