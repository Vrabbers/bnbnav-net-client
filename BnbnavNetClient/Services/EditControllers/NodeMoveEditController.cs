using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.EditControllers;

public class NodeMoveEditController : IEditController
{
    private Node? _movingNode = null;
    private Node? _movedNode = null;

    public PointerPressedFlags PointerPressed(MapView mapView, PointerPressedEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);
        
        if (mapView.HitTest(pointerPos).FirstOrDefault(x => x is Node) is Node node)
        {
            _movingNode = node;
            _movedNode = new("temp", node.X, node.Y, node.Z);
            return PointerPressedFlags.DoNotPan;
        }

        return PointerPressedFlags.None;
    }

    public void PointerMoved(MapView mapView, PointerEventArgs args)
    {
        var pointerPos = args.GetPosition(mapView);

        if (_movingNode is not null && _movedNode is not null)
        {
            var newCoords = mapView.ToWorld(pointerPos);
            _movedNode = new("temp", (int)Math.Round(newCoords.X), _movingNode.Y, (int)Math.Round(newCoords.Y));
            mapView.InvalidateVisual();
        }
    }

    public void PointerReleased(MapView mapView, PointerReleasedEventArgs args)
    {
        if (_movingNode is not null)
        {
            
        }
        
        _movingNode = null;
        _movedNode = null;
    }

    public void Render(MapView mapView, DrawingContext context)
    {
        if (_movingNode is not null && _movedNode is not null)
        {
            var nodeBorder = (Pen)mapView.FindResource("NodeBorder")!;
            var selNodeBrush = (Brush)mapView.FindResource("SelectedNodeFill")!;

            var movingRect = _movingNode.BoundingRect(mapView);
            var movedRect = _movedNode.BoundingRect(mapView);
            
            PolylineGeometry geo = new();
            geo.Points.Add(movingRect.Center);
            geo.Points.Add(movedRect.Center);

            //TODO: Another colour?
            var pen = (Pen)mapView.FindResource("RoadGhostPen")!;
            pen.Thickness = mapView.ThicknessForRoadType(RoadType.Local) * mapView.MapViewModel.Scale;
            context.DrawGeometry(null, pen, geo);

            context.DrawRectangle(selNodeBrush, nodeBorder, _movingNode.BoundingRect(mapView));
            context.DrawRectangle(selNodeBrush, nodeBorder, _movedNode.BoundingRect(mapView));
        }
    }
}