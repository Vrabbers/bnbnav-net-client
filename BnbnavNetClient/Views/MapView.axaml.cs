using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Reactive;
using BnbnavNetClient.Services;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using Svg.Skia;
using System.Collections.Generic;
using System.Linq;
using BnbnavNetClient.Models;
using DynamicData;
using BnbnavNetClient.Helpers;
using Avalonia.Skia;
using Avalonia.Animation;

namespace BnbnavNetClient.Views;

public partial class MapView : UserControl
{
    bool _pointerPressing;
    bool _disablePan = false;
    Point _pointerPrevPosition;

    Matrix _toScreenMtx = Matrix.Identity;
    Matrix _toWorldMtx = Matrix.Identity;

    readonly IAssetLoader _assetLoader;

    MapViewModel MapViewModel => (MapViewModel)DataContext!;

    readonly List<Node> _roadGhosts = new();

    Vector _velocity;

    public MapView()
    {
        _assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>()!;
        InitializeComponent();
        Clock = new Clock();
        Clock.Subscribe(ts =>
        {
            if (_pointerPressing)
                return;
            MapViewModel.Pan += _velocity;
            _velocity /= 1.075;
        });

    }

    protected override void OnInitialized()
    {
        base.OnInitialized();


        PointerPressed += (_, eventArgs) =>
        {
            var pointerPos = eventArgs.GetPosition(this);
            _pointerPressing = true;
            _pointerPrevPosition = pointerPos;

            _disablePan = false;
            switch (MapViewModel.MapEditorService.CurrentEditMode)
            {
                case EditModeControl.Select:
                    MapViewModel.Test = string.Empty;
                    foreach (var hit in HitTest(pointerPos))
                        MapViewModel.Test += hit.ToString() + "\n";
                    break;
                case EditModeControl.Join:
                case EditModeControl.JoinTwoWay:
                    //Don't try to pan
                    if (HitTest(pointerPos).Any(x => x is Node)) _disablePan = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };

        PointerMoved += (_, eventArgs) =>
        {
            var pointerPos = eventArgs.GetPosition(this);

            if (_pointerPressing)
            {
                if (_disablePan)
                {
                    switch (MapViewModel.MapEditorService.CurrentEditMode)
                    {
                        case EditModeControl.Select:
                            break;
                        case EditModeControl.Join:
                        case EditModeControl.JoinTwoWay:
                            var item = HitTest(pointerPos).FirstOrDefault(x => x is Node);
                            if (item is Node node)
                            {
                                var lastNode = _roadGhosts.Last();
                                
                                //Make sure this makes sense
                                //TODO: Make sure we can't loop back on ourselves: we also need to check RoadGhosts for duplicates
                                if (node.Id != lastNode.Id && !MapViewModel.MapService.Edges.Any(x =>
                                        x.Value.From == lastNode && x.Value.To == node))
                                {
                                    _roadGhosts.Add(node);
                                }
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    InvalidateVisual();
                } 
                else
                {
                    _velocity = (_pointerPrevPosition - pointerPos) / MapViewModel.Scale;
                    MapViewModel.Pan += _velocity;
                }
                _pointerPrevPosition = pointerPos;
            }
            else
            {
                switch (MapViewModel.MapEditorService.CurrentEditMode)
                {
                    case EditModeControl.Select:
                        break;
                    case EditModeControl.Join:
                    case EditModeControl.JoinTwoWay:
                    {
                        _roadGhosts.Clear();
                        // Draw a circular ghost around any nodes
                        var item = HitTest(pointerPos).FirstOrDefault(x => x is Node);
                        if (item is Node node)
                        {
                            _roadGhosts.Add(node);
                        }
                        InvalidateVisual();
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        };

        PointerReleased += (_, eventArgs) =>
        {
            _pointerPressing = false;

            foreach (var item in HitTest(eventArgs.GetPosition(this)))
            {
                switch (item)
                {
                    case Node node:
                        Console.WriteLine($"Clicked on Node   x: {node.X}  y: {node.Y}  z: {node.Z}");
                        break;
                    case Landmark landmark:
                        Console.WriteLine($"Clicked on Landmark  type: {landmark.Type}  name: {landmark.Name}");
                        break;
                    case Edge edge:
                        Console.WriteLine($"Clicked on Edge  road: {edge.Road.Name}");
                        break;
                }
            }
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

                UpdateDrawnItems();
            }));
        MapViewModel
            .WhenAnyPropertyChanged()
            .Subscribe(Observer.Create<MapViewModel?>(_ => { InvalidateVisual(); }));
        MapViewModel.MapService
            .WhenAnyPropertyChanged()
            .Subscribe(Observer.Create<MapService?>(_ => InvalidateVisual()));
    }

    static readonly double LandmarkSize = 10;
    static readonly double NodeSize = 14;
    readonly Dictionary<string, SKSvg> _svgCache = new();

    List<(Point, Point, Edge)> _drawnEdges = new List<(Point, Point, Edge)>();
    List<(Rect, Landmark)> _drawnLandmarks = new List<(Rect, Landmark)>();
    List<(Rect, Node)> _drawnNodes = new List<(Rect, Node)>();

    IEnumerable<MapItem> HitTest(Point point)
    {
        foreach (var (rect, landmark) in _drawnLandmarks)
        {
            if (rect.Contains(point)) yield return landmark;
        }
        
        foreach (var (rect, node) in _drawnNodes)
        {
            if (rect.Contains(point)) yield return node;
        }

        foreach (var (a, b, edge) in _drawnEdges)
        {
            if (GeoHelper.LineSegmentToPointDistance(a, b, point) <= ThicknessForRoadType(edge.Road.RoadType) * MapViewModel.Scale / 2)
                yield return edge;
        }
    }


    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateDrawnItems(new Rect(finalSize));
        return base.ArrangeOverride(finalSize);
    }

    void UpdateDrawnItems(Rect? boundsRect = null)
    {
        var mapService = MapViewModel.MapService;
        var scale = MapViewModel.Scale;

        if (Bounds.Size.IsDefault && boundsRect is null)
        {
            return;
        }

        var bounds = boundsRect ?? Bounds;

        _drawnEdges = mapService.Edges.Values.Select(edge =>
        {
            var fromEdge = mapService.Nodes[edge.From.Id];
            var from = ToScreen(new(fromEdge.X, fromEdge.Z));
            var toEdge = mapService.Nodes[edge.To.Id];
            var to = ToScreen(new (toEdge.X, toEdge.Z));
            return (from, to, edge);
        }).Where(edge => GeoHelper.LineIntersects(edge.from, edge.to, bounds)).ToList();

        _drawnLandmarks = mapService.Landmarks.Values.Select(landmark =>
        {
            var pos = ToScreen(new(landmark.Node.X, landmark.Node.Z));
            var rect = new Rect(
                pos.X - LandmarkSize * scale / 2,
                pos.Y - LandmarkSize * scale / 2,
                LandmarkSize * scale, LandmarkSize * scale);
            return (rect, landmark);
        }).Where(landmark => bounds.Intersects(landmark.rect)).ToList();

        _drawnNodes = mapService.Nodes.Values.Select(node =>
        {
            var pos = ToScreen(new(node.X, node.Z));
            var rect = new Rect(
                pos.X - NodeSize / 2, 
                pos.Y - NodeSize / 2,
                NodeSize, NodeSize);
            return (rect, node);
        }).Where(node => bounds.Intersects(node.rect)).ToList();
    }

    Pen PenForRoadType(RoadType type) => (Pen)(type switch
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

    double ThicknessForRoadType(RoadType type) => (double)(type == RoadType.Motorway ? this.FindResource("MotorwayThickness")! : this.FindResource("RoadThickness")!);

    public override void Render(DrawingContext context)
    {
        var scale = MapViewModel.Scale;

        context.FillRectangle((Brush)this.FindResource("BackgroundBrush")!, Bounds);

        foreach (var (from, to, edge) in _drawnEdges)
        {
            var pen = PenForRoadType(edge.Road.RoadType);

            var length = double.Sqrt(double.Pow(from.X - to.X, 2) + double.Pow(from.Y - to.Y, 2));
            var diffPoint = to - from;
            var angle = double.Atan2(diffPoint.Y, diffPoint.X);

            var matrix = Matrix.Identity *
                Matrix.CreateRotation(angle) *
                Matrix.CreateTranslation(from);

            pen.Thickness = ThicknessForRoadType(edge.Road.RoadType) * scale;
            if (pen.Brush is LinearGradientBrush gradBrush)
            {
                gradBrush.StartPoint = new RelativePoint(0, -pen.Thickness / 2, RelativeUnit.Absolute);
                gradBrush.EndPoint = new RelativePoint(0, pen.Thickness / 2, RelativeUnit.Absolute);
            }
            using (context.PushPreTransform(matrix))
                context.DrawLine(pen, new(0, 0), new(length, 0));
        }

        if (scale >= 0.8)
        {
            foreach (var (rect, landmark) in _drawnLandmarks)
            {
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

                        svg = new();
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
            if (_roadGhosts.Count != 0)
            {
                PolylineGeometry geo = new();
                geo.Points.AddRange(_roadGhosts.Select(x => ToScreen(new(x.X, x.Z))));
                if (_pointerPressing) geo.Points.Add(_pointerPrevPosition);
                
                //Make the shape into a circle
                if (geo.Points.Count == 1) geo.Points.Add(geo.Points.First());

                var pen = (Pen)this.FindResource("RoadGhostPen")!;
                pen.Thickness = ThicknessForRoadType(RoadType.Local) * scale;
                context.DrawGeometry(null, pen, geo);
            }
            var nodeBorder = (Pen)this.FindResource("NodeBorder")!;
            var nodeBrush = (Brush)this.FindResource("NodeFill")!;
            var selNodeBrush = (Brush)this.FindResource("SelectedNodeFill")!;
            foreach (var (rect, node) in _drawnNodes)
            {
                var brush = _roadGhosts.Contains(node) ? selNodeBrush : nodeBrush;
                context.DrawRectangle(brush, nodeBorder, rect);
            }
        }

        base.Render(context);
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
