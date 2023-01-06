using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using BnbnavNetClient.Services;
using BnbnavNetClient.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Linq;

namespace BnbnavNetClient.Views;
public class MapRenderer : UserControl
{
    bool _pointerPressing;
    Point _pointerPrevPosition;

    MapService? _mapService;
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (DataContext is not MainViewModel mvm)
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
            _mapService!.Pan += _pointerPrevPosition - pointerPos;
            _pointerPrevPosition = pointerPos;
        };

        PointerReleased += (_, __) =>
        {
            _pointerPressing = false;
        };

        mvm.WhenAnyValue(x => x.MapService).Subscribe(ms =>
        {
            _mapService = ms;
            InvalidateVisual();
            ms.WhenAnyValue(x => x.Pan).Subscribe(_ => InvalidateVisual());
        });

    }

    static readonly IPen BlackBorderPen = new Pen(new SolidColorBrush(Colors.Black), thickness: 2);
    static readonly IBrush BackgroundBrush = new SolidColorBrush(Colors.WhiteSmoke);
    static readonly IBrush WhiteFillBrush = new SolidColorBrush(Colors.White);
    static readonly IPen RoadPen = new Pen(new SolidColorBrush(Colors.DarkBlue), thickness: 20, lineCap: PenLineCap.Round);
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_mapService is null)
            return;
        var pan = _mapService.Pan;

        context.FillRectangle(BackgroundBrush, Bounds);

        foreach (var edge in _mapService.Edges.Values)
        {
            var fromEdge = _mapService.Nodes[edge.From.Id];
            var from = new Point(fromEdge.X, fromEdge.Z) - pan;
            var toEdge = _mapService.Nodes[edge.To.Id];
            var to = new Point(toEdge.X, toEdge.Z) - pan;
            if (!LineIntersects(from, to, Bounds))
                continue;
            context.DrawLine(RoadPen, from, to);
        }

        foreach (var node in _mapService.Nodes.Values)
        {
            var rect = new Rect(node.X - 7 - pan.X, node.Z - 7 - pan.Y, 14, 14);
            if (!Bounds.Intersects(rect))
                continue;
            context.DrawRectangle(WhiteFillBrush, BlackBorderPen, rect);
        }
    }

    static bool LineIntersects(Point from, Point to, Rect Bounds)
    {
        if (Bounds.Contains(from) || Bounds.Contains(to)) return true;
        // TODO: do this properly
        return false;
    }
}
