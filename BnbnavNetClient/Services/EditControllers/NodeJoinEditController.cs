using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class NodeJoinEditController : EditController
{
    readonly MapEditorService _editorService;
    bool _mouseDown = false;
    readonly List<Node> _roadGhosts = new();
    bool _lockRoadGhosts = false;
    Point _pointerPrevPosition;

    public NodeJoinEditController(MapEditorService editorService)
    {
        _editorService = editorService;
    }
    
    public override PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        
        _pointerPrevPosition = pointerPos;
        
        if (mapView.HitTest(pointerPos).Any(x => x is Node))
        {
            _mouseDown = true;
            return PointerPressedFlags.DoNotPan;
        }

        return PointerPressedFlags.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        if (_editorService.MapService is null) return;
        
        var pointerPos = args.GetPosition(mapView);
        
        if (_mouseDown)
        {
            var item = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node);
            if (item is Node node)
            {
                var lastNode = _roadGhosts.Last();
                
                //Make sure this makes sense
                //TODO: Make sure we can't loop back on ourselves: we also need to check RoadGhosts for duplicates
                if (node.Id != lastNode.Id && !_editorService.MapService.Edges.Any(x =>
                        x.Value.From == lastNode && x.Value.To == node))
                {
                    _roadGhosts.Add(node);
                }
            }
        }
        else
        {
            if (!_lockRoadGhosts)
            {
                _roadGhosts.Clear();
                // Draw a circular ghost around any nodes
                var item = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node);
                if (item is Node node)
                {
                    _roadGhosts.Add(node);
                }
                mapView.InvalidateVisual();
            }
        }
        
        _pointerPrevPosition = pointerPos;
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
        if (_editorService.MapService is null) return;

        _mouseDown = false;
        
        //Attempt to join the nodes
        if (_roadGhosts.Count > 1)
        {
            _lockRoadGhosts = true;
            var flyout = mapView.OpenFlyout(new NewEdgeFlyoutViewModel(_editorService, _roadGhosts));
            
            flyout.Closed += (_, _) =>
            {
                _lockRoadGhosts = false;
                _roadGhosts.Clear();
                mapView.InvalidateVisual();
            };
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        if (_roadGhosts.Count != 0)
        {
            PolylineGeometry geo = new();
            geo.Points.AddRange(_roadGhosts.Select(x => mapView.ToScreen(new(x.X, x.Z))));
            if (_mouseDown) geo.Points.Add(_pointerPrevPosition);
                
            //Make the shape into a circle
            if (geo.Points.Count == 1) geo.Points.Add(geo.Points.First());

            var pen = (Pen)mapView.FindResource("RoadGhostPen")!;
            pen.Thickness = mapView.ThicknessForRoadType(RoadType.Local) * mapView.MapViewModel.Scale;
            context.DrawGeometry(null, pen, geo);
        }
        var nodeBorder = (Pen)mapView.FindResource("NodeBorder")!;
        var selNodeBrush = (Brush)mapView.FindResource("SelectedNodeFill")!;
        foreach (var (rect, node) in mapView.DrawnNodes.Where(x => _roadGhosts.Contains(x.Item2)))
        {
            context.DrawRectangle(selNodeBrush, nodeBorder, rect);
        }
    }
}