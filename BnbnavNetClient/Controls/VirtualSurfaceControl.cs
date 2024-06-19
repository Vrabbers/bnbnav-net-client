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

    private readonly Dictionary<TileIndex, Tile> tileMap = [];
    private uint renderSequenceNumber;

    public abstract void DrawTile(TileSurface surface, Rect worldCoordinates);

    public void InvalidateTiles(PixelRect pixelBounds)
    {
        Interlocked.Increment(ref renderSequenceNumber);

        var (topLeftTile, bottomRightTile) = GetExtents(pixelBounds);

        for (int x = topLeftTile.X; x <= bottomRightTile.X; x++)
        {
            for (int y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
            {
                var tileIndex = new TileIndex(x, y);
                ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(tileMap, tileIndex);
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
        Interlocked.Increment(ref renderSequenceNumber);

        foreach (var key in tileMap.Keys)
        {
            ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(tileMap, key);
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

        var originalSequenceNumber = renderSequenceNumber;

        var (panX, panY) = Pan * Scale * dpiScale;

        var viewportRect = renderBounds * dpiScale;
        var pixelBounds = new PixelRect((int)panX, (int)panY, (int)viewportRect.Width, (int)viewportRect.Height);

        var (topLeftTile, bottomRightTile) = GetExtents(pixelBounds);

        canvas.Save();

        canvas.SetMatrix(canvas.TotalMatrix.PostConcat(SKMatrix.CreateScale((float)dpiScale, (float)dpiScale).Invert()));

        for (int x = topLeftTile.X; x <= bottomRightTile.X; x++)
        {
            for (int y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
            {
                var tileIndex = new TileIndex(x, y);
                ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(tileMap, tileIndex, out bool exists);

                if (!exists || tile.Dirty)
                {
                    var surface = tile.Surface ??= SKSurface.Create(lease.GrContext, false, new SKImageInfo(TileSide, TileSide));
                    var surfaceCanvas = surface.Canvas;

                    surfaceCanvas.Save();

                    DrawTile(new TileSurface
                    {
                        Surface = surface,
                        Canvas = surfaceCanvas,
                        CanvasSize = new(TileSide, TileSide)
                    },
                    new Rect(x * TileSide, y * TileSide, TileSide, TileSide) * Scale);

                    surfaceCanvas.Restore();

                    // If a region of the virtual surface was invalidated, the tile we're rendering may be outdated
                    // Another thread is racing to mark those tiles as dirty, so avoid overwriting that
                    // A redraw is queued, so any glitches will be fixed on the next redraw
                    if (originalSequenceNumber == renderSequenceNumber)
                    {
                        tile.Dirty = false;
                    }
                }

                canvas.DrawSurface(tile.Surface, new((tileIndex.X << TileSideExponent) - (float)panX, (tileIndex.Y << TileSideExponent) - (float)panY));
            }
        }
        
        canvas.Restore();
    }

    private Point ToScreen(Point viewportPoint)
    {
        var mtx = Matrix.CreateScale(Scale, Scale) * Matrix.CreateTranslation(-Pan);

        return mtx.Transform(viewportPoint);
    }

    public double Scale { get; set; } = 1;

    private static (TileIndex TopLeft, TileIndex BottomRight) GetExtents(PixelRect pixelBounds)
    {
        var topLeft = new TileIndex(pixelBounds.X >> TileSideExponent, pixelBounds.Y >> TileSideExponent);
        var bottomRight = new TileIndex((pixelBounds.Right >> TileSideExponent) + 1, (pixelBounds.Bottom >> TileSideExponent) + 1);

        return (topLeft, bottomRight);
    }

    private Point pan = new(0, 512);
    public Point Pan
    {
        get => pan;
        set
        {
            pan = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        renderBounds = new(0, 0, Bounds.Width, Bounds.Height);
        context.Custom(this);
    }

    private Rect renderBounds;

    Rect ICustomDrawOperation.Bounds => renderBounds;

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