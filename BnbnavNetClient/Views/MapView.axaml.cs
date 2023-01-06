using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.ViewModels;
using DynamicData.Binding;
using System.Reactive;

namespace BnbnavNetClient.Views;
public partial class MapView : UserControl
{
    bool _pointerPressing;
    Point _pointerPrevPosition;
    MapViewModel MapViewModel => (MapViewModel)DataContext!;


    public MapView()
    {
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
        MapViewModel.WhenAnyPropertyChanged().Subscribe(Observer.Create<MapViewModel?>(_ => { InvalidateVisual(); }));
    }

    static readonly double NodeSize = 14;
    static readonly IPen BlackBorderPen = new Pen(new SolidColorBrush(Colors.Black), thickness: 2);
    static readonly IBrush BackgroundBrush = new SolidColorBrush(Colors.WhiteSmoke);
    static readonly IBrush WhiteFillBrush = new SolidColorBrush(Colors.White);
    static readonly Pen RoadPen = new Pen(new SolidColorBrush(Colors.DarkBlue), thickness: 20, lineCap: PenLineCap.Round);
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var pan = MapViewModel.Pan;
        var scale = MapViewModel.Scale;
        var mapService = MapViewModel.MapService;

        context.FillRectangle(BackgroundBrush, Bounds);

        foreach (var edge in mapService.Edges.Values)
        {
            var fromEdge = mapService.Nodes[edge.From.Id];
            var from = (new Point(fromEdge.X, fromEdge.Z) - pan) * scale;
            var toEdge = mapService.Nodes[edge.To.Id];
            var to = (new Point(toEdge.X, toEdge.Z) - pan) * scale;
            if (!LineIntersects(from, to, Bounds))
                continue;
            RoadPen.Thickness = 20 * scale;
            context.DrawLine(RoadPen, from, to);
        }

        foreach (var node in mapService.Nodes.Values)
        {
            var rect = new Rect(
                (node.X - pan.X) * scale - NodeSize / 2, 
                (node.Z - pan.Y) * scale - NodeSize / 2,
                NodeSize, NodeSize);
            if (!Bounds.Intersects(rect))
                continue;
            context.DrawRectangle(WhiteFillBrush, BlackBorderPen, rect);
        }
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

    static Point ToStaticWorld(Point screenCoords, Point pan, double scale) =>
        screenCoords / scale + pan;

    static Point ToStaticScreen(Point worldCoords, Point pan, double scale) =>
        scale * (worldCoords - pan);

    Point ToWorld(Point screenCoords) =>
         ToStaticWorld(screenCoords, MapViewModel.Pan, MapViewModel.Scale);

    Point ToScreen(Point worldCoords) =>
        ToStaticScreen(worldCoords, MapViewModel.Pan, MapViewModel.Scale);

    public void Zoom(double deltaScale, Point origin)
    {
        var newScale = double.Clamp(MapViewModel.Scale + deltaScale, 0.1, 5.0);
        
        var worldPrevPos = ToWorld(origin);
        var worldFutureIncorrectPos = ToStaticWorld(origin, MapViewModel.Pan, newScale);
        var correction = worldFutureIncorrectPos - worldPrevPos;
        MapViewModel.Pan -= correction;
        MapViewModel.Scale = newScale;
    }
}
