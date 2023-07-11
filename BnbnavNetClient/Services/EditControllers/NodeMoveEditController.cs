using System.Linq;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services.NetworkOperations;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class NodeMoveEditController : EditController
{
    readonly MapEditorService _editorService;
    Node? _movingNode;
    Node? _movedNode;

    public NodeMoveEditController(MapEditorService editorService)
    {
        _editorService = editorService;
    }

    public override PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        
        if (mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node) is Node node)
        {
            _movingNode = node;
            _movedNode = new Node("temp", node.X, node.Y, node.Z, node.World);
            return PointerPressedFlags.DoNotPan;
        }

        return PointerPressedFlags.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);

        if (_movingNode is not null && _movedNode is not null)
        {
            var newCoords = mapView.ToWorld(pointerPos);
            _movedNode = new Node("temp", (int)double.Round(newCoords.X), _movingNode.Y, (int)double.Round(newCoords.Y), _movedNode.World);
            mapView.InvalidateVisual();
        }
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
        if (_movingNode is not null && _movedNode is not null)
        {
            _editorService.TrackNetworkOperation(new NodeMoveOperation(_editorService, _movingNode, _movedNode));
        }
        
        _movingNode = null;
        _movedNode = null;
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        if (_movingNode is not null && _movedNode is not null)
        {
            new NodeMoveOperation(null, _movingNode, _movedNode).Render(mapView, context);
        }
    }
}