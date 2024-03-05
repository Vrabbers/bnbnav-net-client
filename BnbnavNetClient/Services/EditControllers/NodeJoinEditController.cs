using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.ViewModels;
using BnbnavNetClient.Views;
using DynamicData;
using DynamicData.Kernel;

namespace BnbnavNetClient.Services.EditControllers;

public class NodeJoinEditController(MapEditorService editorService) : EditController
{
    bool _mouseDown;
    readonly List<Node> _roadGhosts = [];
    bool _lockRoadGhosts;
    Point _pointerPrevPosition;
    Node? _firstNode;
    Node? _hoveredNode;
    bool _nodeSet = true;

    bool AppendRoadGhost(Node node)
    {
        var lastNode = _roadGhosts.Last();

        //TODO: Make sure we can't loop back on ourselves: we also need to check RoadGhosts for duplicates
        if (node.Id == lastNode.Id || editorService.MapService!.Edges.Any(x =>
                x.Value.From == lastNode && x.Value.To == node))
        {
            return false;
        }

        _roadGhosts.Add(node);
        return true;

    }
    
    public override PointerPressed PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        
        _pointerPrevPosition = pointerPos;

        var pointerNode = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node) as Node;

        if (_firstNode is not null)
        {
            // Attempt to join two nodes
            if (pointerNode is null)
            {
                return EditControllers.PointerPressed.None;
            }

            if (pointerNode == _firstNode)
            {
                _firstNode = null;
                _nodeSet = false;
                _roadGhosts.Clear();
                return EditControllers.PointerPressed.DoNotPan;
            }

            if (!AppendRoadGhost(pointerNode))
            {
                return EditControllers.PointerPressed.DoNotPan;
            }
            
            _firstNode = null;
            _nodeSet = false;

            mapView.InvalidateVisual();
            
            _lockRoadGhosts = true;
            var flyout = mapView.OpenFlyout(new NewEdgeFlyoutViewModel(editorService, _roadGhosts));

            if (flyout is not null)
            {
                flyout.Closed += (_, _) =>
                {
                    _lockRoadGhosts = false;
                    _roadGhosts.Clear();
                    mapView.InvalidateVisual();
                };
            }
            
            return EditControllers.PointerPressed.DoNotPan;
        }
        
        if (pointerNode is not null)
        {
            _mouseDown = true;
            _firstNode = pointerNode;
            _roadGhosts.Add(pointerNode);
            mapView.InvalidateVisual();
            return EditControllers.PointerPressed.DoNotPan;
        }

        return EditControllers.PointerPressed.None;
    }

    public override void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        if (editorService.MapService is null) return;
        
        var pointerPos = args.GetPosition(mapView);
        
        if (_mouseDown)
        {
            _hoveredNode = null;
            
            var item = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node);
            if (item is Node node)
            {
                //Make sure this makes sense
                AppendRoadGhost(node);
            }
        }
        else
        {
            if (!_lockRoadGhosts)
            {
                // Draw a circular ghost around any nodes
                _hoveredNode = mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node) as Node;
                mapView.InvalidateVisual();
            }
        }
        
        _pointerPrevPosition = pointerPos;
    }

    public override void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
        if (editorService.MapService is null) return;

        var pointerPos = args.GetPosition(mapView);
        _mouseDown = false;
        
        //Attempt to join the nodes
        if (_roadGhosts.Count > 1)
        {
            _lockRoadGhosts = true;
            var flyout = mapView.OpenFlyout(new NewEdgeFlyoutViewModel(editorService, _roadGhosts));

            if (flyout is not null)
            {
                flyout.Closed += (_, _) =>
                {
                    _lockRoadGhosts = false;
                    _firstNode = null;
                    _nodeSet = false;
                    _roadGhosts.Clear();
                    mapView.InvalidateVisual();
                };
            }

            return;
        }

        // Determine if we're clicking on nodes
        if (mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node) is Node n && _firstNode is not null)
        {
            if (_firstNode != n)
            {
                _firstNode = null;
                _roadGhosts.Clear();
            }

            _nodeSet = true;
            return;
        }

        if (!_nodeSet)
        {
            _firstNode = null;
            _roadGhosts.Clear();
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        var ghosts = _roadGhosts.AsList();
        if (_hoveredNode is not null && !_mouseDown) ghosts = ghosts.Append(_hoveredNode).AsList();
        
        if (ghosts.Count != 0)
        {
            PolylineGeometry geo = new();
            geo.Points.AddRange(ghosts.Select(x => mapView.ToScreen(new Point(x.X, x.Z))));
            if (_mouseDown) geo.Points.Add(_pointerPrevPosition);
                
            //Make the shape into a circle
            if (geo.Points.Count == 1) geo.Points.Add(geo.Points.First());

            var pen = (Pen)mapView.ThemeDict["RoadGhostPen"]!;
            pen.Thickness = mapView.ThicknessForRoadType(RoadType.Local) * mapView.MapViewModel.Scale;
            context.DrawGeometry(null, pen, geo);
        }
        var nodeBorder = (Pen)mapView.ThemeDict["NodeBorder"]!;
        var selNodeBrush = (Brush)mapView.ThemeDict["SelectedNodeFill"]!;
        foreach (var (rect, _) in mapView.DrawnNodes.Where(x => ghosts.Contains(x.Item2)))
        {
            context.DrawRectangle(selNodeBrush, nodeBorder, rect);
        }
    }
}