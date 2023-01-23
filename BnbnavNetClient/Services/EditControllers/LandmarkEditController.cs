using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class LandmarkEditController : EditController
{
    private readonly MapEditorService _mapEditorService;
    private Point _pointerPrevPosition;
    private Point _initialPointerPosition;

    public LandmarkEditController(MapEditorService mapEditorService)
    {
        _mapEditorService = mapEditorService;
    }
    
    public override PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        _pointerPrevPosition = pointerPos;
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
            }.Length < 0.5)
        {
            var mapItem = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node);
            if (mapItem is Node node)
            {
                mapView.OpenFlyout(new LandmarkFlyoutViewModel(_mapEditorService, node));
            }
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        
    }
}