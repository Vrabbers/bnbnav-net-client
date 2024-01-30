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
    readonly MapEditorService _mapEditorService;
    Point _initialPointerPosition;

    public SelectEditController(MapEditorService mapEditorService)
    {
        _mapEditorService = mapEditorService;
    }
    
    public override PointerPressed PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        _initialPointerPosition = pointerPos;

        return EditControllers.PointerPressed.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);

        if (new ExtendedLine(pointerPos, _initialPointerPosition).Length >= 0.5) return;
            
        var item = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Edge or Node);
        switch (item)
        {
            case Edge edge:
                mapView.OpenFlyout(new RoadEditViewModel(_mapEditorService, edge.Road));
                break;
            case Node node:
                if (_mapEditorService.MapService!.Landmarks.Values.Any(x => x.Node == node))
                    mapView.OpenFlyout(new LandmarkFlyoutViewModel(_mapEditorService, node));

                break;
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        
    }
}