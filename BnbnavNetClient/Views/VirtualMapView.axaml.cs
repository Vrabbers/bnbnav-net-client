using Avalonia;
using Avalonia.Input;
using Avalonia.Skia;

using BnbnavNetClient.Controls;
using BnbnavNetClient.Models;
using BnbnavNetClient.ViewModels;

using SkiaSharp;

using System;
using System.Diagnostics;

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

        var pinchRecognizer = new PinchGestureRecognizer();
        GestureRecognizers.Add(pinchRecognizer);
        Gestures.AddPinchHandler(this, HandlePinchForZoom);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var currentPosition = e.GetCurrentPoint(this);

        if (currentPosition.Properties.IsLeftButtonPressed)
        {
            Pan += (previousPointerPosition - currentPosition.Position) / Scale;
            MapViewModel.Pan = Pan;
        }

        previousPointerPosition = currentPosition.Position;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var currentPosition = e.GetCurrentPoint(this);
        previousPointerPosition = currentPosition.Position;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var currentPosition = e.GetPosition(this);

        var deltaScale = e.Delta.Y * Scale / 10.0;
        Zoom(deltaScale, e.GetPosition(this));
    }

    private double previousPinchScale = 1;
    public void HandlePinchForZoom(object? sender, PinchEventArgs e)
    {
        Zoom(e.Scale - previousPinchScale, e.ScaleOrigin);
        previousPinchScale = e.Scale;
    }

    // private double scale = 1;

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

    private MapViewResources resources = new()
    {
        BackgroundColor = SKColors.White,
        RoutePen = new SKPaint { Color = SKColors.Green },
        LocalRoadPen = new SKPaint { Color = SKColors.CornflowerBlue },
        MainRoadPen       = new SKPaint { Color = SKColors.CornflowerBlue },
        HighwayRoadPen    = new SKPaint { Color = SKColors.CornflowerBlue },
        ExpresswayRoadPen = new SKPaint { Color = SKColors.CornflowerBlue },
        MotorwayRoadPen   = new SKPaint { Color = SKColors.CornflowerBlue },
        FootpathRoadPen   = new SKPaint { Color = SKColors.CornflowerBlue },
        WaterwayRoadPen   = new SKPaint { Color = SKColors.CornflowerBlue },
        PrivateRoadPen    = new SKPaint { Color = SKColors.CornflowerBlue },
        RoundaboutRoadPen = new SKPaint { Color = SKColors.CornflowerBlue },
        DuongWarpRoadPen  = new SKPaint { Color = SKColors.CornflowerBlue },
        UnknownRoadPen    = new SKPaint { Color = SKColors.CornflowerBlue },

        MotorwayThickness = 10,
        RoadThickness = 5,
    };

    public MapViewModel MapViewModel { get; set; }

    public override void DrawTile(TileSurface surface, Rect worldCoordinates)
    {
        Debug.WriteLine($"Rendering tile {worldCoordinates.TopLeft} with scale " + Scale);

        var canvas = surface.Canvas;

        canvas.DrawColor(new SKColor((uint)Random.Shared.Next()));

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
            //if (noRender.Contains(edge))
            //    continue;

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
        var pen = drawRoute ? resources.RoutePen : resources.PaintForRoadType(roadType);
        //new Pen(new SolidColorBrush(new Color(255, 0, 150, 255)), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round) : PenForRoadType(roadType);

        var length = SKPoint.Distance(from, to);
        var diffPoint = to - from;
        var angle = float.Atan2(diffPoint.Y, diffPoint.X);

        var matrix = SKMatrix.CreateRotation(angle)
            .PostConcat(SKMatrix.CreateTranslation(from.X, from.Y));

        pen.StrokeWidth = (float)resources.ThicknessForRoadType(roadType);

        canvas.DrawLine(matrix.MapPoint(new(0, 0)), matrix.MapPoint(new(length, 0)), pen);
    }

}


public sealed class MapViewResources
{
    public required SKColor BackgroundColor { get; init; }

    public required SKPaint RoutePen { get; init; }

    public required SKPaint LocalRoadPen { get; init; }
    public required SKPaint MainRoadPen { get; init; }
    public required SKPaint HighwayRoadPen { get; init; }
    public required SKPaint ExpresswayRoadPen { get; init; }
    public required SKPaint MotorwayRoadPen { get; init; }
    public required SKPaint FootpathRoadPen { get; init; }
    public required SKPaint WaterwayRoadPen { get; init; }
    public required SKPaint PrivateRoadPen { get; init; }
    public required SKPaint RoundaboutRoadPen { get; init; }
    public required SKPaint DuongWarpRoadPen { get; init; }
    public required SKPaint UnknownRoadPen { get; init; }


    public SKPaint PaintForRoadType(RoadType type) => type switch
    {
        RoadType.Local => LocalRoadPen,
        RoadType.Main => MainRoadPen,
        RoadType.Highway => HighwayRoadPen,
        RoadType.Expressway => ExpresswayRoadPen,
        RoadType.Motorway => MotorwayRoadPen,
        RoadType.Footpath => FootpathRoadPen,
        RoadType.Waterway => WaterwayRoadPen,
        RoadType.Private => PrivateRoadPen,
        RoadType.Roundabout => RoundaboutRoadPen,
        RoadType.DuongWarp => DuongWarpRoadPen,
        _ => UnknownRoadPen,
    };

    public required double MotorwayThickness { get; init; }

    public required double RoadThickness { get; init; }

    public double ThicknessForRoadType(RoadType type) => type == RoadType.Motorway ? MotorwayThickness : RoadThickness;

}