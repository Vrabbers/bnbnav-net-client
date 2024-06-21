using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Skia;

using BnbnavNetClient.Controls;
using BnbnavNetClient.Models;
using BnbnavNetClient.ViewModels;

using SkiaSharp;

namespace BnbnavNetClient.Views;

internal partial class VirtualMapView : VirtualSurfaceControl
{
    public VirtualMapView()
    {
        InitializeComponent();
    }

    private Point previousPointerPosition;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        //<LinearGradientBrush StartPoint="-100%,0%" EndPoint="100%, 0%">
        //    <GradientStop Color="#640000" Offset="0"/>
        //    <GradientStop Color="#640000" Offset="0.3"/>
        //    <GradientStop Color="#c8c800" Offset="0.3001"/>
        //    <GradientStop Color="#c8c800" Offset="0.6999"/>
        //    <GradientStop Color="#640000" Offset="0.7"/>
        //    <GradientStop Color="#640000" Offset="1"/>
        //  </LinearGradientBrush>
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var currentPosition = e.GetCurrentPoint(this);

        if (currentPosition.Properties.IsLeftButtonPressed)
        {
            Pan += (previousPointerPosition - currentPosition.Position) / Scale;
            MapViewModel.Pan = Pan;
            InvalidateVisual();
        }

        previousPointerPosition = currentPosition.Position;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var currentPosition = e.GetCurrentPoint(this);
        previousPointerPosition = currentPosition.Position;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var deltaScale = e.Delta.Y * Scale / 10.0;
        Zoom(deltaScale, e.GetPosition(this));
    }

    private double previousPinchScale = 1;
    public void HandlePinchForZoom(object? sender, PinchEventArgs e)
    {
        Zoom(e.Scale - previousPinchScale, e.ScaleOrigin);
        previousPinchScale = e.Scale;
    }

    public void HandlePinchEndedForZoom(object? sender, PinchEndedEventArgs e)
    {
        previousPinchScale = 1;
    }

    public void Zoom(double deltaScale, Point origin)
    {
        var clampedScale = double.Clamp(Scale + deltaScale, 0.1, 20.0);

        var worldPrevPos = ToWorld(origin);

        Scale = clampedScale;

        var worldFutureIncorrectPos = ToWorld(origin);

        var correction = worldFutureIncorrectPos - worldPrevPos;
        Pan -= correction;
        MapViewModel.Pan = Pan;

        InvalidateTiles();
    }

    private Point ToWorld(Point viewportPoint)
    {
        return (viewportPoint / Scale) + Pan;
    }

    public MapViewModel MapViewModel { get; set; }

    public override void DrawTile(TileSurface surface, Rect worldCoordinates)
    {
        ThemeResources = (IResourceDictionary)this.FindResource(ActualThemeVariant.ToString())!;

        var canvas = surface.Canvas;
        canvas.Clear();

        var noRender = new List<MapItem>();
        noRender.AddRange(MapViewModel.MapEditorService.EditController.ItemsNotToRender);

        //bool lockTaken = false;
        //try
        //{

        //    if (lockTaken = Monitor.TryEnter(mapView.MapEditorService.OngoingNetworkOperationsMutex))
        //    {
        //        foreach (var operation in mapView.MapEditorService.OngoingNetworkOperations)
        //        {
        //            noRender.AddRange(operation.ItemsNotToRender);
        //            operation.Render(this, context);
        //        }
        //    }
        //}
        //finally
        //{
        //    if (lockTaken)
        //        Monitor.Exit(mapView.MapEditorService.OngoingNetworkOperationsMutex);
        //}

        // var landmarks = map.Landmarks.Values.Where(landmark => landmark.Node.World == mapView.ChosenWorld).Select(landmark => (landmark.BoundingRect(this), landmark))
        //.Where(landmark => bounds.Intersects(landmark.Item1)).ToList();

        //var spiedNodes = nodes.Where(node =>
        // {
        //     return MapViewModel.HighlightInterWorldNodesEnabled && mapService.AllEdges.Where(edge => edge.From.Id == node.Id || edge.To.Id == node.Id).Any(edge => edge.From.World != edge.To.World);
        // }).ToList();
        
        var map = MapViewModel.MapService;

        var nodes = new List<Node>();
        var edges = new List<Edge>();

        var queryRegion = new IntRect((int)worldCoordinates.Left, (int)worldCoordinates.Top, (int)worldCoordinates.Right, (int)worldCoordinates.Bottom);

        map.MapBins.Query(queryRegion, nodes, edges);

        foreach (var edge in edges)
        {
            var from = edge.From.Point.ToSKPoint();
            var to = edge.To.Point.ToSKPoint();

            DrawEdge(canvas, edge.Road.RoadType, from, to, drawRoute: false);
        }

        //foreach (var (rect, landmark) in DrawnLandmarks)
        //{
        //    if (noRender.Contains(landmark)) continue;
        //    DrawLandmark(context, landmark, rect);
        //}

        //if (MapViewModel.CurrentUi == AvailableUi.Go)
        //{
        //    //Draw the arrow indicator
        //    var instruction = MapViewModel.MapService.CurrentRoute?.CurrentInstruction;
        //    if (instruction is { From: not null, To: not null })
        //    {
        //        var pen = new Pen(new SolidColorBrush(new Color(255, 100, 50, 150)),
        //            PenForRoadType(RoadType.Local).Thickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        //        var poly = new PolylineGeometry(new[]
        //        {
        //            (instruction.From.Line.FlipDirection().SetLength(10).Point2),
        //            (instruction.Node.Point),
        //            (instruction.To.Line.SetLength(10).Point2),
        //            (instruction.To.Line.SetLength(10).FlipDirection().NudgeAngle(-45).SetLength(5).Point2),
        //            (instruction.To.Line.SetLength(10).Point2),
        //            (instruction.To.Line.SetLength(10).FlipDirection().NudgeAngle(45).SetLength(5).Point2),
        //        }, false);
        //        context.DrawGeometry(null, pen, poly);
        //    }
        //}

        //if (MapViewModel.IsInEditMode)
        //{
        //    var nodeBorder = (Pen)ThemeDict["NodeBorder"]!;
        //    var nodeBrush = (Brush)ThemeDict["NodeFill"]!;
        //    var spiedBorder = (Pen)ThemeDict["SpiedNodeBorder"]!;
        //    var spiedBrush = (Brush)ThemeDict["SpiedNodeFill"]!;
        //    foreach (var node in DrawnNodes)
        //    {
        //        var rect = node.BoundingRect(this);
        //        if (noRender.Contains(node)) continue;

        //        if (SpiedNodes.Any(spied => spied.Id == node.Id))
        //        {
        //            context.DrawRectangle(spiedBrush, spiedBorder, rect);
        //        }
        //        else
        //        {
        //            context.DrawRectangle(nodeBrush, nodeBorder, rect);
        //        }
        //    }

        //    MapViewModel.MapEditorService.EditController.Render(this, context);
        //}

        //foreach (var player in MapViewModel.MapService.Players.Values
        //             .Where(player => player.World == MapViewModel.ChosenWorld))
        //{
        //    var rect = GeometryHelper.SquareCenteredOn((player.MarkerCoordinates), PlayerSize);
        //    const string? uriString = "avares://BnbnavNetClient/Assets/playermark.svg";
        //    context.DrawSvgUrl(uriString, rect, -player.MarkerAngle + MapViewModel.Rotation);

        //    //Draw the player name
        //    var textBrush = (Brush)ThemeDict["ForegroundBrush"]!;

        //    if (player.PlayerText is null)
        //    {
        //        player.GeneratePlayerText(FontFamily);
        //    }
        //    player.PlayerText!.SetForegroundBrush(textBrush);

        //    var textCenter = rect.Center + new Point(0, rect.Height / 2 + 8 + player.PlayerText.Height / 2);
        //    context.DrawText(player.PlayerText, textCenter - new Point(player.PlayerText.Width, player.PlayerText.Height) / 2);

        //    if (player.SnappedEdge is null)
        //        continue;

        //    var roadText = new FormattedText(player.SnappedEdge.Road.Name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
        //        new Typeface(FontFamily), 16, new SolidColorBrush(new Color(255, 255, 255, 255)));

        //    var roadCenter = textCenter + new Point(0, player.PlayerText.Height / 2 + 8 + roadText.Height / 2);
        //    var roadRect = new Rect(roadCenter - new Point(roadText.Width + 10, roadText.Height) / 2,
        //        new Size(roadText.Width + 10, roadText.Height));
        //    var backingRect = roadRect.Inflate(3);
        //    context.DrawRectangle(new SolidColorBrush(new Color(255, 0, 120, 130)), null, backingRect,
        //        backingRect.Height / 2, backingRect.Height / 2);
        //    context.DrawText(roadText, roadCenter - new Point(roadText.Width / 2, roadText.Height / 2));


        //}


    }

    private void DrawEdge(SKCanvas canvas, RoadType roadType, SKPoint from, SKPoint to, bool drawGhost = false, bool drawRoute = false)
    {
        var pen = drawRoute ? Get<SKPaint>("RoutePen") : PaintForRoadType(roadType);

        var length = SKPoint.Distance(from, to);
        var diffPoint = to - from;
        var angle = float.Atan2(diffPoint.Y, diffPoint.X);

        var matrix = SKMatrix.CreateRotation(angle)
            .PostConcat(SKMatrix.CreateTranslation(from.X, from.Y));

        pen.StrokeWidth = (float)ThicknessForRoadType(roadType);

        canvas.DrawLine(matrix.MapPoint(new(0, 0)), matrix.MapPoint(new(length, 0)), pen);
    }

    private SKPaint PaintForRoadType(RoadType type) => type switch
    {
        RoadType.Local => Get<SKPaint>("LocalRoadPen"),
        RoadType.Main => Get<SKPaint>("MainRoadPen"),
        RoadType.Highway => Get<SKPaint>("HighwayRoadPen"),
        RoadType.Expressway => Get<SKPaint>("ExpresswayRoadPen"),
        RoadType.Motorway => Get<SKPaint>("MotorwayRoadPen"),
        RoadType.Footpath => Get<SKPaint>("FootpathRoadPen"),
        RoadType.Waterway => Get<SKPaint>("WaterwayRoadPen"),
        RoadType.Private => Get<SKPaint>("PrivateRoadPen"),
        RoadType.Roundabout => Get<SKPaint>("RoundaboutRoadPen"),
        RoadType.DuongWarp => Get<SKPaint>("DuongWarpRoadPen"),
        _ => Get<SKPaint>("UnknownRoadPen"),
    };

    private double ThicknessForRoadType(RoadType type) => type switch
    {
        RoadType.Motorway => Get<double>("MotorwayThickness"),
        _ => Get<double>("RoadThickness")
    };

    private IResourceDictionary ThemeResources;

    private T Get<T>(string resourceKey)
    {
        return (T)ThemeResources[resourceKey]!;
    }


}
