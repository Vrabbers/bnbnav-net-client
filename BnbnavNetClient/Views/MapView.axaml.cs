using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using BnbnavNetClient.Services;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using Svg.Skia;
using System.Collections.Generic;
using BnbnavNetClient.Models;
using DynamicData;
using BnbnavNetClient.Helpers;
using Avalonia.Controls.Primitives;
using System.Collections.Immutable;
using Avalonia.Threading;
using BnbnavNetClient.Services.EditControllers;
using BnbnavNetClient.Services.NetworkOperations;

namespace BnbnavNetClient.Views;

public partial class MapView : UserControl
{
    bool _pointerPressing;
    bool _disablePan = false;
    Point _pointerPrevPosition;
    Vector _viewVelocity = Vector.Zero;
    readonly List<Point> _pointerVelocities = new();
    // This list is averaged to get smooth panning.

    Matrix _toScreenMtx = Matrix.Identity;
    Matrix _toWorldMtx = Matrix.Identity;

    readonly IAssetLoader _assetLoader;

    public MapViewModel MapViewModel => (MapViewModel)DataContext!;

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
            var pointerPos = eventArgs.GetPosition(this);
            var pointer = eventArgs.GetCurrentPoint(this);

            if (pointer.Properties.IsRightButtonPressed)
            {
                MapViewModel.LastRightClickHitTest = HitTest(pointerPos).ToList();
                return;
            }
            _pointerPressing = true;
            _pointerPrevPosition = pointerPos;

            _disablePan = false;

            var flags = MapViewModel.MapEditorService.EditController.PointerPressed(this, eventArgs);

            _disablePan = flags.HasFlag(PointerPressedFlags.DoNotPan);
                
            _viewVelocity = Vector.Zero;
            _pointerVelocities.Clear();
        };

        PointerMoved += (_, eventArgs) =>
        {
            var pointerPos = eventArgs.GetPosition(this);
            
            MapViewModel.MapEditorService.EditController.PointerMoved(this, eventArgs);

            if (_pointerPressing)
            {
                if (_disablePan)
                {
                    InvalidateVisual();
                } 
                else
                {
                    // We need to pan _more_ when scale is smaller:
                    MapViewModel.Pan += (_pointerPrevPosition - pointerPos) / MapViewModel.Scale;
                    
                    _pointerVelocities.Add(_pointerPrevPosition - pointerPos);

                    if (_pointerVelocities.Count > 5)
                        _pointerVelocities.RemoveAt(0);

                    var xAverage = _pointerVelocities.Average(i => i.X);
                    var yAverage = _pointerVelocities.Average(i => i.Y);

                    _viewVelocity = new(xAverage, yAverage);

                    if (Math.Abs(_viewVelocity.Y) < 7 && Math.Abs(_viewVelocity.Y) < 7)
                        _viewVelocity = Vector.Zero;

                    // The actual view velocity should be the average of the last 5
                    // computed velocities, due to how low-quality mouses work (low-quality
                    // mouses have a tendency to move in angles snapped to 45 degrees).
                }
                _pointerPrevPosition = pointerPos;
            }
        };

        PointerReleased += (_, eventArgs) =>
        {
            _pointerPressing = false;

            MapViewModel.MapEditorService.EditController.PointerReleased(this, eventArgs);

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
            _pointerVelocities.Clear(); // Make sure we're not using velocities from previous pan.
        };

        PointerWheelChanged += (_, eventArgs) =>
        {
            var deltaScale = eventArgs.Delta.Y * MapViewModel.Scale / 10.0;
            Zoom(deltaScale, (eventArgs.GetPosition(this)));

            _viewVelocity = Vector.Zero; // Reset velocities
            _pointerVelocities.Clear();
        };

        // This is the physics part of inertial panning.
        Clock = new Clock();
        Clock.Subscribe(
            ts =>
            {
                if (_pointerPressing)
                    return;

                // Stop the timer, don't waste resources.
                if (Math.Abs(_viewVelocity.X) < 4 && Math.Abs(_viewVelocity.Y) < 4)
                    _viewVelocity = Vector.Zero;
                else
                    MapViewModel.Pan += _viewVelocity / MapViewModel.Scale;

                _viewVelocity /= 1.075; // 1.075 is the friction.
            }
        );

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
            .Subscribe(Observer.Create<MapService?>(_ => UpdateDrawnItems()));
        MapViewModel.MapEditorService
            .WhenAnyValue(x => x.OngoingNetworkOperations)
            .Subscribe(Observer.Create<IReadOnlyList<NetworkOperation>?>(_ => Dispatcher.UIThread.Post(InvalidateVisual)));
    }

    static readonly double LandmarkSize = 10;
    readonly Dictionary<string, SKSvg> _svgCache = new();

    private List<(Point, Point, Edge)> DrawnEdges { get; set; } = new();
    private List<(Rect, Landmark)> DrawnLandmarks { get; set; } = new();
    public List<(Rect, Node)> DrawnNodes { get; set; } = new();

    public IEnumerable<MapItem> HitTest(Point point)
    {
        foreach (var (rect, landmark) in DrawnLandmarks)
        {
            if (rect.Contains(point)) yield return landmark;
        }
        
        foreach (var (rect, node) in DrawnNodes)
        {
            if (rect.Contains(point)) yield return node;
        }

        foreach (var (a, b, edge) in DrawnEdges)
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

        DrawnEdges = mapService.Edges.Values.Select(edge =>
        {
            var (from, to) = edge.Extents(this);
            return (from, to, edge);
        }).Where(edge => GeoHelper.LineIntersects(edge.from, edge.to, bounds)).ToList();

        DrawnLandmarks = mapService.Landmarks.Values.Select(landmark =>
        {
            var pos = ToScreen(new(landmark.Node.X, landmark.Node.Z));
            var rect = new Rect(
                pos.X - LandmarkSize * scale / 2,
                pos.Y - LandmarkSize * scale / 2,
                LandmarkSize * scale, LandmarkSize * scale);
            return (rect, landmark);
        }).Where(landmark => bounds.Intersects(landmark.rect)).ToList();

        DrawnNodes = mapService.Nodes.Values.Select(node =>
        {
            return (node.BoundingRect(this), node);
        }).Where(node => bounds.Intersects(node.Item1)).ToList();
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

    public double ThicknessForRoadType(RoadType type) => (double)(type == RoadType.Motorway ? this.FindResource("MotorwayThickness")! : this.FindResource("RoadThickness")!);

    public void DrawEdge(DrawingContext context, RoadType roadType, Point from, Point to, bool drawGhost = false)
    {
            
        var pen = PenForRoadType(roadType);

        var length = double.Sqrt(double.Pow(from.X - to.X, 2) + double.Pow(from.Y - to.Y, 2));
        var diffPoint = to - from;
        var angle = double.Atan2(diffPoint.Y, diffPoint.X);

        var matrix = Matrix.Identity *
                     Matrix.CreateRotation(angle) *
                     Matrix.CreateTranslation(from);

        pen.Thickness = ThicknessForRoadType(roadType) * MapViewModel.Scale;
        if (pen.Brush is LinearGradientBrush gradBrush)
        {
            gradBrush.StartPoint = new RelativePoint(0, -pen.Thickness / 2, RelativeUnit.Absolute);
            gradBrush.EndPoint = new RelativePoint(0, pen.Thickness / 2, RelativeUnit.Absolute);
        }
        using (context.PushPreTransform(matrix))
            using (context.PushOpacity(drawGhost ? 0.5 : 1))
                context.DrawLine(pen, new(0, 0), new(length, 0));
    }

    public override void Render(DrawingContext context)
    {
        var scale = MapViewModel.Scale;

        context.FillRectangle((Brush)this.FindResource("BackgroundBrush")!, Bounds);

        var noRender = new List<MapItem>();
        noRender.AddRange(MapViewModel.MapEditorService.EditController.ItemsNotToRender);
        foreach (var operation in MapViewModel.MapEditorService.OngoingNetworkOperations)
        {
            noRender.AddRange(operation.ItemsNotToRender);
        }

        foreach (var (from, to, edge) in DrawnEdges)
        {
            if (noRender.Contains(edge)) continue;
            DrawEdge(context, edge.Road.RoadType, from, to);
        }

        if (scale >= 0.8)
        {
            foreach (var (rect, landmark) in DrawnLandmarks)
            {
                if (noRender.Contains(landmark)) continue;
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
            var nodeBorder = (Pen)this.FindResource("NodeBorder")!;
            var nodeBrush = (Brush)this.FindResource("NodeFill")!;
            foreach (var (rect, node) in DrawnNodes)
            {
                if (noRender.Contains(node)) continue;
                context.DrawRectangle(nodeBrush, nodeBorder, rect);
            }
            
            MapViewModel.MapEditorService.EditController.Render(this, context);
        }

        foreach (var operation in MapViewModel.MapEditorService.OngoingNetworkOperations)
        {
            operation.Render(this, context);
        }

        base.Render(context);
    }

    public Point ToWorld(Point screenCoords) =>
         _toWorldMtx.Transform(screenCoords);

    public Point ToScreen(Point worldCoords) =>
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

    public FlyoutBase OpenFlyout(ViewModel viewModel)
    {
        MapViewModel.FlyoutViewModel = viewModel;
        var flyout = Flyout.GetAttachedFlyout(this);
        flyout!.ShowAt(this, showAtPointer: true);
        if (viewModel is IOpenableAsFlyout ioaf) ioaf.Flyout = flyout;
        return flyout;
    }
}
