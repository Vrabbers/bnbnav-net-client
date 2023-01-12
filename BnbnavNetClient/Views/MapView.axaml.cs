using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Xml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.Svg.Skia;
using SkiaSharp;
using Svg;
using Svg.Skia;

namespace BnbnavNetClient.Views;
public partial class MapView : UserControl
{
    bool _pointerPressing;
    Point _pointerPrevPosition;
    MapViewModel MapViewModel => (MapViewModel)DataContext!;

    Matrix _toScreenMtx = Matrix.Identity;
    Matrix _toWorldMtx = Matrix.Identity;

    private IAssetLoader _assetLoader;

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
            .WhenAnyPropertyChanged()
            .Subscribe(Observer.Create<MapViewModel?>(_ => { InvalidateVisual(); }));
    }

    static readonly double NodeSize = 14;
    private static readonly double LandmarkSize = 10;
    static readonly IPen BlackBorderPen = new Pen(new SolidColorBrush(Colors.Black), thickness: 2);
    static readonly IBrush BackgroundBrush = new SolidColorBrush(Colors.WhiteSmoke);
    static readonly IBrush WhiteFillBrush = new SolidColorBrush(Colors.White);
    static readonly Pen RoadPen = new Pen(new SolidColorBrush(Colors.DarkBlue), thickness: 20, lineCap: PenLineCap.Round);
    public override void Render(DrawingContext context)
    {
        var mapService = MapViewModel.MapService;
        var scale = MapViewModel.Scale;

        context.FillRectangle(BackgroundBrush, Bounds);

        foreach (var edge in mapService.Edges.Values)
        {
            var fromEdge = mapService.Nodes[edge.From.Id];
            var from = ToScreen(new(fromEdge.X, fromEdge.Z));
            var toEdge = mapService.Nodes[edge.To.Id];
            var to = ToScreen(new (toEdge.X, toEdge.Z));
            if (!LineIntersects(from, to, Bounds))
                continue;
            RoadPen.Thickness = 20 * scale;
            context.DrawLine(RoadPen, from, to);
        }
        
        foreach (var landmark in mapService.Landmarks.Values)
        {
            
            var pos = ToScreen(new(landmark.Node.X, landmark.Node.Z));
            var rect = new Rect(
                pos.X - LandmarkSize * scale / 2, 
                pos.Y - LandmarkSize * scale / 2,
                LandmarkSize * scale, LandmarkSize * scale);
            if (!Bounds.Intersects(rect))
                continue;

            try
            {
                var asset = _assetLoader.Open(new($"avares://BnbnavNetClient/Assets/Landmarks/{landmark.Type}.svg"));

                var svg = new SKSvg();
                svg.Load(asset);
                if (svg.Picture is null) continue;
                
                var sourceSize = new Size(svg.Picture.CullRect.Width, svg.Picture.CullRect.Height);
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
            catch (FileNotFoundException)
            {
                //Ignore
            }
        }

        if (MapViewModel.IsInEditMode)
        {
            foreach (var node in mapService.Nodes.Values)
            {
                var pos = ToScreen(new(node.X, node.Z));
                var rect = new Rect(
                    pos.X - NodeSize / 2, 
                    pos.Y - NodeSize / 2,
                    NodeSize, NodeSize);
                if (!Bounds.Intersects(rect))
                    continue;
                context.DrawRectangle(WhiteFillBrush, BlackBorderPen, rect);
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
