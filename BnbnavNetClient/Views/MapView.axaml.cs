using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.ViewModels;
using DynamicData.Binding;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;
using System;
using System.Reactive;

namespace BnbnavNetClient.Views;
public partial class MapView : UserControl
{
    bool _pointerPressing;
    Point _pointerPrevPosition;

    MapTheme _currentTheme;

    MapViewModel MapViewModel => (MapViewModel)DataContext!;

    Matrix _toScreenMtx = Matrix.Identity;
    Matrix _toWorldMtx = Matrix.Identity;

    public MapView()
    {
        InitializeComponent();
        _currentTheme = new DayTheme();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        PointerPressed += (_, eventArgs) =>
        {
            _pointerPressing = true;
            _pointerPrevPosition = eventArgs.GetPosition(this);
        };

        PointerMoved += (_, eventArgs) =>
        {
            if (!_pointerPressing)
                return;

            var pointerPos = eventArgs.GetPosition(this);

            // We need to pan _more_ when scale is smaller:
            MapViewModel.Pan += (_pointerPrevPosition - pointerPos) / MapViewModel.Scale;
            _pointerPrevPosition = pointerPos;
        };

        PointerReleased += (_, __) =>
        {
            _pointerPressing = false;
        };

        PointerWheelChanged += (_, eventArgs) =>
        {
            var deltaScale = eventArgs.Delta.Y / 10.0;
            Zoom(deltaScale, (eventArgs.GetPosition(this)));
        };

        //why does this happen to me :sob:
        MapViewModel
            .WhenAnyValue(x => x.Pan, x => x.Scale, x => x.Rotation)
            .Subscribe(Observer.Create<ValueTuple<Point, double, double>>(tuple =>
            {
                var pan = tuple.Item1;
                var scale = tuple.Item2;
                var rotate = tuple.Item3;

                var matrix =
                    Matrix.CreateTranslation(-pan) *
                    Matrix.CreateScale(scale, scale);

                if (rotate != 0)
                {
                    var centerOfBounds = new Vector(Bounds.Width, Bounds.Height) / (scale * 2);
                    
                    matrix *=
                        Matrix.CreateTranslation(-centerOfBounds) *
                        Matrix.CreateRotation(rotate) * 
                        Matrix.CreateTranslation(centerOfBounds);
                }

                _toScreenMtx = matrix;
                _toWorldMtx = matrix.Invert();
            }));

        MapViewModel
            .WhenAnyValue(x => x.NightMode)
            .Subscribe(night =>
            {
                _currentTheme = night ? new NightTheme() : new DayTheme();
            });

        MapViewModel
            .WhenAnyPropertyChanged()
            .Subscribe(Observer.Create<MapViewModel?>(_ => { InvalidateVisual(); }));
    }

    public override void Render(DrawingContext context)
    {
        var mapService = MapViewModel.MapService;
        var scale = MapViewModel.Scale;

        context.FillRectangle(_currentTheme.BackgroundBrush, Bounds);

        foreach (var edge in mapService.Edges.Values)
        {
            var pen = _currentTheme.PlaceholderRoad;
            var fromEdge = mapService.Nodes[edge.From.Id];
            var from = ToScreen(new(fromEdge.X, fromEdge.Z));
            var toEdge = mapService.Nodes[edge.To.Id];
            var to = ToScreen(new (toEdge.X, toEdge.Z));
            if (!LineIntersects(from, to, Bounds))
                continue;
            pen.Thickness = 20 * scale;
            context.DrawLine(pen, from, to);
        }

        if (MapViewModel.IsInEditMode)
        {
            var nodeSize = _currentTheme.NodeSize;
            foreach (var node in mapService.Nodes.Values)
            {
                var pos = ToScreen(new(node.X, node.Z));
                var rect = new Rect(
                    pos.X - nodeSize / 2, 
                    pos.Y - nodeSize / 2,
                    nodeSize, nodeSize);
                if (!Bounds.Intersects(rect))
                    continue;
                context.DrawRectangle(_currentTheme.NodeFill, _currentTheme.NodeBorder, rect);
            }
        }

        base.Render(context);
    }

    static bool LineIntersects(Point from, Point to, Rect Bounds)
    {
        if (Bounds.Contains(from) || Bounds.Contains(to)) 
            return true;

        //If the line is bigger than the smallest edge of the bounds, draw it, as both points may lie outside the view;
        var minDistSqr = double.Pow(double.Min(Bounds.Width, Bounds.Height), 2);
        var lengthSqr = double.Pow(from.X - to.X, 2) + double.Pow(from.Y - to.Y, 2);
        if (lengthSqr > minDistSqr) 
            return true;
        // TODO: do this properly
        return false;
    }

    Point ToWorld(Point screenCoords) =>
         _toWorldMtx.Transform(screenCoords);

    Point ToScreen(Point worldCoords) =>
        _toScreenMtx.Transform(worldCoords);

    public void Zoom(double deltaScale, Point origin)
    {
        var newScale = double.Clamp(MapViewModel.Scale + deltaScale, 0.1, 5.0);
        
        var worldPrevPos = ToWorld(origin);
        MapViewModel.Scale = newScale;
        var worldFutureIncorrectPos = ToWorld(origin);
        var correction = worldFutureIncorrectPos - worldPrevPos;
        MapViewModel.Pan -= correction;
    }
}


//TODO: use resources?
abstract class MapTheme
{
    public virtual double NodeSize => 14;
    public abstract Brush BackgroundBrush { get; }
    public abstract Brush NodeFill { get; }
    public abstract Pen NodeBorder { get; }
    public abstract Pen PlaceholderRoad { get; }
}

sealed class DayTheme : MapTheme
{
    public override Brush BackgroundBrush => new SolidColorBrush(Colors.WhiteSmoke);
    public override Brush NodeFill => new SolidColorBrush(Colors.White);
    public override Pen NodeBorder => new(new SolidColorBrush(Colors.Black), thickness: 2);
    public override Pen PlaceholderRoad => new(new SolidColorBrush(Colors.DarkBlue), thickness: 20, lineCap: PenLineCap.Round);
}

sealed class NightTheme : MapTheme
{
    public override Brush BackgroundBrush => new SolidColorBrush(Colors.Black);
    public override Brush NodeFill => new SolidColorBrush(Colors.Black);
    public override Pen NodeBorder => new(new SolidColorBrush(Colors.White), thickness: 2);
    public override Pen PlaceholderRoad => new(new SolidColorBrush(Colors.Blue), thickness: 20, lineCap: PenLineCap.Round);
}