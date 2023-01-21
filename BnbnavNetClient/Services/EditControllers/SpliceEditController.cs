using System;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class SpliceEditController : EditController
{
    private readonly MapEditorService _editorService;
    private Point _pointerPrevPosition;
    private Edge? _splicingEdge;
    private Node? _splicingAt;
    private bool _mouseDown;

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
        if (_splicingEdge is not null)
        {
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
                tempNode = new("temp", (int)Math.Round(coords.X), _splicingEdge.From.Y, (int)Math.Round(coords.Y));
            }

            var edge1 = _splicingEdge with
            {
                To = tempNode
            };
            var edge2 = _splicingEdge with
            {
                From = tempNode
            };
            var (from1, to1) = edge1.Extents(mapView);
            var (from2, to2) = edge2.Extents(mapView);
            
            mapView.DrawEdge(context, edge1.Road.RoadType, from1, to1, true);
            mapView.DrawEdge(context, edge1.Road.RoadType, from2, to2, true);
        }
    }
}