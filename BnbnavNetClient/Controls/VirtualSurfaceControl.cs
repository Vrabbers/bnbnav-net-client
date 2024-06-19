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

internal abstract class VirtualSurfaceControl : Control
{
    private struct Tile { public SKSurface Surface; public bool Dirty; }
    private readonly record struct TileIndex(int X, int Y);

    private const int TileSideExponent = 9;
    private const int TileSide = 1 << TileSideExponent; // 512

    private readonly Dictionary<TileIndex, Tile> _tileMap = [];
    
    private uint _renderSequenceNumber;

    public double Scale { get; set; } = 1;
    public Point Pan { get; set; }

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

        foreach (var tileIndex in _tileMap.Keys)
        {
            ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(_tileMap, tileIndex);
            tile.Dirty = true;
        }

        InvalidateVisual();
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
        if (VisualRoot is not TopLevel { RenderScaling: var dpiScale })
        {
            return;
        }

        var (panX, panY) = Pan * Scale * dpiScale;

        var viewportRect = Bounds * dpiScale;
        var pixelBounds = new PixelRect((int)panX, (int)panY, (int)viewportRect.Width, (int)viewportRect.Height);

        var (topLeftTile, bottomRightTile) = GetExtents(pixelBounds);

        var dirtyTiles = new Dictionary<TileIndex, SKPicture>();

        for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
        {
            for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
            {
                var tileIndex = new TileIndex(x, y);
                ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(_tileMap, tileIndex, out var exists);

                if (!exists || tile.Dirty)
                {
                    var recorder = new SKPictureRecorder();
                    var surfaceCanvas = recorder.BeginRecording(new SKRect(0, 0, TileSide, TileSide));

                    var worldCoordinates = new Rect(x * TileSide, y * TileSide, TileSide, TileSide) * (1 / (Scale * dpiScale));

                    surfaceCanvas.Scale((float)(Scale * dpiScale));
                    surfaceCanvas.Translate((-worldCoordinates.TopLeft).ToSKPoint());

                    DrawTile(new TileSurface { Canvas = surfaceCanvas, CanvasSize = new(TileSide, TileSide) }, worldCoordinates);

                    tile.Dirty = false;

                    dirtyTiles.Add(tileIndex, recorder.EndRecording());
                }
            }
        }

        context.Custom(new VirtualSurfaceRenderOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), Pan, Scale, dpiScale, _tileMap, dirtyTiles));
    }
    
    private class VirtualSurfaceRenderOperation(
        Rect renderBounds,
        Point pan,
        double scale,
        double dpiScale,
        Dictionary<TileIndex, Tile> tileMap,
        Dictionary<TileIndex, SKPicture> dirtyTiles) : ICustomDrawOperation
    {
        public Rect Bounds => renderBounds;

        public bool HitTest(Point p) => true;

        public bool Equals(ICustomDrawOperation? other) => this == other;

        public void Dispose()
        {
        }

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            var (panX, panY) = pan * scale * dpiScale;

            var viewportRect = renderBounds * dpiScale;
            var pixelBounds = new PixelRect((int)panX, (int)panY, (int)viewportRect.Width, (int)viewportRect.Height);

            var (topLeftTile, bottomRightTile) = GetExtents(pixelBounds);

            canvas.Save();

            canvas.SetMatrix(canvas.TotalMatrix.PostConcat(SKMatrix.CreateScale((float)dpiScale, (float)dpiScale).Invert()));

            for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
            {
                for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
                {
                    var tileIndex = new TileIndex(x, y);
                    ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(tileMap, tileIndex, out _);

                    if (dirtyTiles.TryGetValue(tileIndex, out var dirtyTilePicture))
                    {
                        var surface = tile.Surface ??= SKSurface.Create(lease.GrContext, false, new SKImageInfo(TileSide, TileSide));
                        var surfaceCanvas = surface.Canvas;

                        surfaceCanvas.Save();

                        surfaceCanvas.DrawPicture(dirtyTilePicture);

                        surfaceCanvas.Restore();
                    }

                    canvas.DrawSurface(tile.Surface, new SKPoint((tileIndex.X << TileSideExponent) - (float)panX, (tileIndex.Y << TileSideExponent) - (float)panY));
                }
            }

            canvas.Restore();
        }
    }
}

internal readonly struct TileSurface
{
    public SKCanvas Canvas { get; init; }

    public SKSizeI CanvasSize { get; init; }
}
