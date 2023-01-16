using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Reactive;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using Svg.Skia;
using System.Collections.Generic;

namespace BnbnavNetClient.Views;
public partial class MapView : UserControl
{
    bool _pointerPressing;
    Point _pointerPrevPosition;

    Matrix _toScreenMtx = Matrix.Identity;
    Matrix _toWorldMtx = Matrix.Identity;

    readonly IAssetLoader _assetLoader;

    MapViewModel MapViewModel => (MapViewModel)DataContext!;

    public MapView()
    {
        _assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>()!;
        InitializeComponent();
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
            var deltaScale = eventArgs.Delta.Y * MapViewModel.Scale / 10.0;
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
            .WhenAnyPropertyChanged()
            .Subscribe(Observer.Create<MapViewModel?>(_ => { InvalidateVisual(); }));
    }

    static readonly double LandmarkSize = 10;
    readonly Dictionary<string, SKSvg> _svgCache = new();

    public override void Render(DrawingContext context)
    {
        var mapService = MapViewModel.MapService;
        var scale = MapViewModel.Scale;

        context.FillRectangle((Brush)this.FindResource("BackgroundBrush")!, Bounds);

        foreach (var edge in mapService.Edges.Values)
        {
            var pen = (Pen)(edge.Road.RoadType switch
            {
                RoadType.Local => this.FindResource("LocalRoadPen")!,
                RoadType.Main => this.FindResource("MainRoadPen")!,
                RoadType.Highway => this.FindResource("HighwayRoadPen")!,
                RoadType.Expressway => this.FindResource("ExpresswayRoadPen")!,
                RoadType.Motorway => this.FindResource("MotorwayRoadPen")!,
                RoadType.Footpath => this.FindResource("FootpathRoadPen")!,
                RoadType.Waterway => this.FindResource("WaterwayRoadPen")!,
                RoadType.Private => this.FindResource("PrivateRoadPen")!,
                RoadType.Roundabout => this.FindResource("RoundaboutRoadPen")!,
                RoadType.DuongWarp => this.FindResource("DuongWarpRoadPen")!,
                _ => this.FindResource("UnknownRoadPen")!,
            });  
            var fromEdge = mapService.Nodes[edge.From.Id];
            var from = ToScreen(new(fromEdge.X, fromEdge.Z));
            var toEdge = mapService.Nodes[edge.To.Id];
            var to = ToScreen(new (toEdge.X, toEdge.Z));      

            var length = double.Sqrt(double.Pow(from.X - to.X, 2) + double.Pow(from.Y - to.Y, 2));
            var diffPoint = to - from;
            var angle = double.Atan2(diffPoint.Y, diffPoint.X);
            
            var matrix = Matrix.Identity *
                Matrix.CreateRotation(angle) *
                Matrix.CreateTranslation(from);
            if (!LineIntersects(from, to, Bounds))
                continue;

            pen.Thickness *= scale;
            if (pen.Brush is LinearGradientBrush gradBrush)
            {
                gradBrush.StartPoint = new RelativePoint(0, -pen.Thickness / 2, RelativeUnit.Absolute);
                gradBrush.EndPoint = new RelativePoint(0, pen.Thickness / 2, RelativeUnit.Absolute);
            }
            using (context.PushPreTransform(matrix))
                context.DrawLine(pen, new(0, 0), new(length, 0));
            pen.Thickness /= scale;
        }

        if (scale >= 0.8)
        {
            foreach (var landmark in mapService.Landmarks.Values)
            {
                var pos = ToScreen(new(landmark.Node.X, landmark.Node.Z));
                var rect = new Rect(
                    pos.X - LandmarkSize * scale / 2,
                    pos.Y - LandmarkSize * scale / 2,
                    LandmarkSize * scale, LandmarkSize * scale);
                if (!Bounds.Intersects(rect))
                    continue;

                SKSvg? svg = null;

                if (_svgCache.TryGetValue(landmark.Type, out var outSvg))
                {
                    svg = outSvg;
                }
                else
                {
                    var uri = new Uri($"avares://BnbnavNetClient/Assets/Landmarks/{landmark.Type}.svg");
                    if (_assetLoader.Exists(uri))
                    {
                        var asset = _assetLoader.Open(uri);

                        svg = new SKSvg();
                        svg.Load(asset);
                        if (svg.Picture is null)
                            continue;
                        _svgCache.Add(landmark.Type, svg);
                    }
                }

                if (svg is null)
                    continue;

                var sourceSize = new Size(svg.Picture!.CullRect.Width, svg.Picture.CullRect.Height);
                var scaleMatrix = Matrix.CreateScale(
                    rect.Width / sourceSize.Width,
                    rect.Height / sourceSize.Height);
                var translateMatrix = Matrix.CreateTranslation(
                    rect.X * sourceSize.Width / rect.Width,
                    rect.Y * sourceSize.Height / rect.Height);

                using (context.PushClip(rect))
                using (context.PushPreTransform(translateMatrix * scaleMatrix))
                    context.Custom(new SvgCustomDrawOperation(rect, svg));

            }
        }

        if (MapViewModel.IsInEditMode)
        {
            var nodeSize = 14;
            foreach (var node in mapService.Nodes.Values)
            {
                var pos = ToScreen(new(node.X, node.Z));
                var rect = new Rect(
                    pos.X - nodeSize / 2, 
                    pos.Y - nodeSize / 2,
                    nodeSize, nodeSize);
                if (!Bounds.Intersects(rect))
                    continue;
                context.DrawRectangle((Brush)this.FindResource("NodeFill")!, (Pen)this.FindResource("NodeBorder")!, rect);
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