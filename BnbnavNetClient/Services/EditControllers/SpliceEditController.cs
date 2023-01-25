using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services.NetworkOperations;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class SpliceEditController : EditController
{
    readonly MapEditorService _editorService;
    Point _pointerPrevPosition;
    Edge? _splicingEdge;
    Node? _splicingAt;
#pragma warning disable CS0414 // The field is assigned but its value is never used
    bool _mouseDown;
#pragma warning restore CS0414 // C# compiler seems to be having a little weird moment here?

    public SpliceEditController(MapEditorService _editorService)
    {
        this._editorService = _editorService;
    }

    public override PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        if (_editorService.MapService is null) return PointerPressedFlags.None;
        
        var pointerPos = args.GetPosition(mapView);
        
        _pointerPrevPosition = pointerPos;
        
        if (mapView.HitTest(pointerPos).FirstOrDefault(x => x is Edge) is Edge edge)
        {
            _mouseDown = true;
            _splicingEdge = edge;
            ItemsNotToRender.Add(_splicingEdge);
            if (_editorService.MapService.OppositeEdge(_splicingEdge) is { } opposite)
            {
                ItemsNotToRender.Add(opposite);
            }
            return PointerPressedFlags.DoNotPan;
        }

        return PointerPressedFlags.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        _pointerPrevPosition = pointerPos;

        if (_splicingEdge is not null)
        {
            if (mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node) is Node node && node != _splicingEdge.From && node != _splicingEdge.To)
            {
                _splicingAt = node;
            }
            else
            {
                _splicingAt = null;
            }
        }
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
        if (_editorService.MapService is null) return;
        
        if (_splicingEdge is not null)
        {
            if (_splicingAt is not null)
            {
                var otherEdge = _editorService.MapService.OppositeEdge(_splicingEdge);
                
                _editorService.TrackNetworkOperation(new EdgeCreateOperation(_editorService, _splicingEdge.Road, _splicingEdge.From, _splicingAt, otherEdge is not null));
                _editorService.TrackNetworkOperation(new EdgeCreateOperation(_editorService, _splicingEdge.Road, _splicingAt, _splicingEdge.To, otherEdge is not null));
                
                _editorService.TrackNetworkOperation(new EdgeDeleteOperation(_editorService, _splicingEdge));
                if (otherEdge is not null) _editorService.TrackNetworkOperation(new EdgeDeleteOperation(_editorService, otherEdge));
            }
            _splicingEdge = null;
        }
        ItemsNotToRender.Clear();
        mapView.InvalidateVisual();
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        if (_splicingEdge is not null)
        {
            var tempNode = _splicingAt;
            if (tempNode is null)
            {
                var coords = mapView.ToWorld(_pointerPrevPosition);
                tempNode = new("temp", (int)double.Round(coords.X), _splicingEdge.From.Y, (int)double.Round(coords.Y));
            }

            var edge1 = new Edge("temp", _splicingEdge.Road, _splicingEdge.From, tempNode);
            var edge2 = new Edge("temp", _splicingEdge.Road, tempNode, _splicingEdge.To);
            var (from1, to1) = edge1.Extents(mapView);
            var (from2, to2) = edge2.Extents(mapView);
            
            mapView.DrawEdge(context, edge1.Road.RoadType, from1, to1, true);
            mapView.DrawEdge(context, edge1.Road.RoadType, from2, to2, true);
        }
    }
}