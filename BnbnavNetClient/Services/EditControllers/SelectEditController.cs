using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class SelectEditController : EditController
{
    private readonly MapEditorService _mapEditorService;
    private Point _initialPointerPosition;

    public SelectEditController(MapEditorService mapEditorService)
    {
        _mapEditorService = mapEditorService;
    }
    
    public override PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        _initialPointerPosition = pointerPos;

        return PointerPressedFlags.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);

        if (new ExtendedLine
            {
                Point1 = pointerPos,
                Point2 = _initialPointerPosition
            }.Length >= 0.5) return;
            
        var item = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Edge);
        if (item is Edge edge)
        {
            mapView.OpenFlyout(new RoadEditViewModel(_mapEditorService, edge.Road));
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        
    }
}