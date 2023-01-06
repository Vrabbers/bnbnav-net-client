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

    public MapView()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (DataContext is not MapViewModel mapViewModel)
            return;
        
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
            mapViewModel.Pan += (_pointerPrevPosition - pointerPos) / mapViewModel.Scale;
            _pointerPrevPosition = pointerPos;
        };

        PointerReleased += (_, __) =>
        {
            _pointerPressing = false;
        };

        PointerWheelChanged += (_, eventArgs) =>
        {
            var factor = 1.0 + 10 * (eventArgs.Delta.Y / 100.0);
            Zoom(factor, (eventArgs.GetPosition(this)));
        };

        //why does this happen to me :sob:
        mapViewModel.WhenAnyPropertyChanged().Subscribe(Observer.Create<MapViewModel?>(_ => { InvalidateVisual(); }));
    }

    static readonly double NodeSize = 14;
    static readonly IPen BlackBorderPen = new Pen(new SolidColorBrush(Colors.Black), thickness: 2);
    static readonly IBrush BackgroundBrush = new SolidColorBrush(Colors.WhiteSmoke);
    static readonly IBrush WhiteFillBrush = new SolidColorBrush(Colors.White);
    static readonly IPen RoadPen = new Pen(new SolidColorBrush(Colors.DarkBlue), thickness: 20, lineCap: PenLineCap.Round);
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (DataContext is not MapViewModel mapViewModel)
            return;

        var pan = mapViewModel.Pan;
        var scale = mapViewModel.Scale;
        var mapService = mapViewModel.MapService;

        context.FillRectangle(BackgroundBrush, Bounds);

        foreach (var edge in mapService.Edges.Values)
        {
            var fromEdge = mapService.Nodes[edge.From.Id];
            var from = (new Point(fromEdge.X, fromEdge.Z) - pan) * scale;
            var toEdge = mapService.Nodes[edge.To.Id];
            var to = (new Point(toEdge.X, toEdge.Z) - pan) * scale;
            if (!LineIntersects(from, to, Bounds))
                continue;
            context.DrawLine(RoadPen, from, to);
        }

        if (mapViewModel.IsInEditMode)
        {
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
    }

    static bool LineIntersects(Point from, Point to, Rect Bounds)
    {
        if (Bounds.Contains(from) || Bounds.Contains(to)) return true;

        // TODO: do this properly
        return false;
    }

    public Point ToWorld(Point pixelCoordinates)
    {
        if (DataContext is not MapViewModel mapViewModel)
            return new Point();
        return pixelCoordinates * mapViewModel.Scale + mapViewModel.Pan;
    }

    public void Zoom(double factor, Point origin)
    {
        if (DataContext is not MapViewModel mapViewModel)
            return;
        var newScale = mapViewModel.Scale * factor;
        if (newScale < 0.1) newScale = 0.1;
        if (newScale > 50) newScale = 50;
        factor = newScale / mapViewModel.Scale;

        mapViewModel.Pan = (mapViewModel.Pan - origin) * factor + origin;
        mapViewModel.Scale = newScale;
    }
}
