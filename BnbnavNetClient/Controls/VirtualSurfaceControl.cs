using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using SkiaSharp;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BnbnavNetClient.Controls;

internal abstract class VirtualSurfaceControl : Control, ICustomDrawOperation
{
    private struct Tile { public SKSurface Surface; public bool Dirty; }
    private readonly record struct TileIndex(int X, int Y);

    private const int TileSideExponent = 9;
    private const int TileSide = 1 << TileSideExponent; // 256

    private readonly Dictionary<TileIndex, Tile> _tileMap = [];
    
    private uint _renderSequenceNumber;

    private Rect _renderBounds;
    private double _scale = 0.5;
    
    public double Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            InvalidateVisual();
        }
    }

    private Point _pan = new(0, 0);
    public Point Pan
    {
        get => _pan;
        set
        {
            _pan = value;
            InvalidateVisual();
        }
    }

    public void PanAndScale(Point pan, double scale)
    {
        _pan = pan;
        _scale = scale;
        InvalidateVisual();
    }
    
    public abstract void DrawTile(TileSurface surface, Rect worldCoordinates);

    public void InvalidateTiles(Rect worldBounds)
    {
        Interlocked.Increment(ref _renderSequenceNumber);

        var (topLeftTile, bottomRightTile) = GetWorldExtends(worldBounds);

        for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
        {
            for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
            {
                var tileIndex = new TileIndex(x, y);
                ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(_tileMap, tileIndex);
                if (!Unsafe.IsNullRef(ref tile))
                {
                    tile.Dirty = true;
                }
            }
        }

        // TODO: Only invalidate visual if this would invalidate on-screen tiles
        InvalidateVisual();
    }

    public void InvalidateTiles()
    {
        Interlocked.Increment(ref _renderSequenceNumber);

        foreach (var key in _tileMap.Keys)
        {
            ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(_tileMap, key);
            tile.Dirty = true;
        }

        InvalidateVisual();
    }


    void ICustomDrawOperation.Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature is null)
        {
            return;
        }

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        if (VisualRoot is not TopLevel { RenderScaling: var dpiScale })
        {
            return;
        }

        var originalSequenceNumber = _renderSequenceNumber;

        var (panX, panY) = Pan * Scale * dpiScale;

        var viewportRect = _renderBounds * dpiScale;
        var pixelBounds = new PixelRect((int)panX, (int)panY, (int)viewportRect.Width, (int)viewportRect.Height);

        var (topLeftTile, bottomRightTile) = GetExtents(pixelBounds);

        canvas.Save();

        canvas.SetMatrix(canvas.TotalMatrix.PostConcat(SKMatrix.CreateScale((float)dpiScale, (float)dpiScale).Invert()));

        for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
        {
            for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
            {
                var tileIndex = new TileIndex(x, y);
                ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(_tileMap, tileIndex, out var exists);

                if (!exists || tile.Dirty)
                {
                    var surface = tile.Surface ??= SKSurface.Create(lease.GrContext, false, new SKImageInfo(TileSide, TileSide));
                    var surfaceCanvas = surface.Canvas;
                    var worldCoordinates = new Rect(x * TileSide, y * TileSide, TileSide, TileSide) * (1 / (Scale * dpiScale));

                    surfaceCanvas.Save();
                    
                    surfaceCanvas.Scale((float)(Scale * dpiScale));
                    surfaceCanvas.Translate((-worldCoordinates.TopLeft).ToSKPoint());

                    DrawTile(new TileSurface
                    {
                        Surface = surface,
                        Canvas = surfaceCanvas,
                        CanvasSize = new SKSizeI(TileSide, TileSide)
                    }, worldCoordinates);

                    surfaceCanvas.Restore();

                    // If a region of the virtual surface was invalidated, the tile we're rendering may be outdated
                    // Another thread is racing to mark those tiles as dirty, so avoid overwriting that
                    // A redraw is queued, so any glitches will be fixed on the next redraw
                    if (originalSequenceNumber == _renderSequenceNumber)
                    {
                        tile.Dirty = false;
                    }
                }

                canvas.DrawSurface(tile.Surface, new SKPoint((tileIndex.X << TileSideExponent) - (float)panX, (tileIndex.Y << TileSideExponent) - (float)panY));
            }
        }
        
        canvas.Restore();
    }

    private Point ToScreen(Point viewportPoint)
    {
        var mtx = Matrix.CreateScale(Scale, Scale) * Matrix.CreateTranslation(-Pan);

        return mtx.Transform(viewportPoint);
    }
    
    private static (TileIndex TopLeft, TileIndex BottomRight) GetExtents(PixelRect pixelBounds)
    {
        var topLeft = new TileIndex(pixelBounds.X >> TileSideExponent, pixelBounds.Y >> TileSideExponent);
        var bottomRight = new TileIndex((pixelBounds.Right >> TileSideExponent) + 1, (pixelBounds.Bottom >> TileSideExponent) + 1);

        return (topLeft, bottomRight);
    }

    private static (TileIndex TopLeft, TileIndex BottomRight) GetWorldExtends(Rect worldBounds)
    {
        var top = (int)double.Floor(worldBounds.Bottom / TileSide);
        var left = (int)double.Floor(worldBounds.Left / TileSide);
        var bottom = (int)double.Ceiling(worldBounds.Bottom / TileSide);
        var right = (int)double.Ceiling(worldBounds.Right / TileSide);

        return (new TileIndex(left, top), new TileIndex(right, bottom));
    }
    
    public override void Render(DrawingContext context)
    {
        _renderBounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        context.Custom(this);
    }
    
    Rect ICustomDrawOperation.Bounds => _renderBounds;

    bool ICustomDrawOperation.HitTest(Point p) => true;
    bool IEquatable<ICustomDrawOperation>.Equals(ICustomDrawOperation? other) => this == other;

    public void Dispose()
    {
    }
}

internal struct TileSurface
{
    public SKSurface Surface { get; init; }
    public SKCanvas Canvas { get; init; }

    public SKSizeI CanvasSize { get; init; }
}