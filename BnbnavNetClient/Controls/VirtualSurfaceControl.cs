using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using SkiaSharp;

using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BnbnavNetClient.Controls;

internal abstract class VirtualSurfaceControl : Control
{
    private struct Tile { public bool Dirty; public long Timestamp; }
    private readonly record struct TileIndex(int X, int Y);
    private readonly record struct TileRect(TileIndex TopLeft, TileIndex BottomRight)
    {
        public readonly bool Contains(TileIndex p)
        {
            return p.X >= TopLeft.X && p.X <= BottomRight.X
                && p.Y >= TopLeft.Y && p.Y <= BottomRight.Y;
        }
    }

    private const int TileSideExponent = 9;
    private const int TileSide = 1 << TileSideExponent; // 512

    private readonly Dictionary<TileIndex, Tile> _tileMap = [];
    private readonly Dictionary<TileIndex, SKSurface> _surfaceMap = [];

    public double Scale { get; set; } = 1;
    public Point Pan { get; set; }

    public VirtualSurfaceControl()
    {
        for (var i = 0; i < _tileStandbyLists.Length; i++)
        {
            _tileStandbyLists[i] = [];
        }
    }

    public abstract void DrawTile(TileSurface surface, Rect worldCoordinates);

    public void InvalidateTiles(Rect worldBounds)
    {
        var visibleTiles = GetWorldExtends(worldBounds);
        var (topLeftTile, bottomRightTile) = visibleTiles;

        bool needsRedraw = false;
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

                if (visibleTiles.Contains(tileIndex))
                {
                    needsRedraw = true;
                }
            }
        }

        if (needsRedraw)
        {
            InvalidateVisual();
        }
    }

    public void InvalidateTiles()
    {
        foreach (var tileIndex in _tileMap.Keys)
        {
            ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(_tileMap, tileIndex);
            tile.Dirty = true;
        }

        InvalidateVisual();
    }

    private double DpiScale()
    {
        if (VisualRoot is TopLevel { RenderScaling: var dpiScale })
        {
            return dpiScale;
        }

        return 1;
    }

    private static TileRect GetTileExtents(Rect renderBounds, Point pan, double scale, double dpiScale)
    {
        var (panX, panY) = pan * scale * dpiScale;

        var viewportRect = renderBounds * dpiScale;
        var pixelBounds = new PixelRect((int)panX, (int)panY, (int)viewportRect.Width, (int)viewportRect.Height);

        var topLeft = new TileIndex(pixelBounds.X >> TileSideExponent, pixelBounds.Y >> TileSideExponent);
        var bottomRight = new TileIndex((pixelBounds.Right >> TileSideExponent) + 1, (pixelBounds.Bottom >> TileSideExponent) + 1);

        return new(topLeft, bottomRight);
    }

    // This function is wrong
    private static TileRect GetWorldExtends(Rect worldBounds)
    {
        var top = (int)double.Floor(worldBounds.Bottom / TileSide);
        var left = (int)double.Floor(worldBounds.Left / TileSide);
        var bottom = (int)double.Ceiling(worldBounds.Bottom / TileSide);
        var right = (int)double.Ceiling(worldBounds.Right / TileSide);

        return new TileRect(new(left, top), new(right, bottom));
    }

    private const int TileStandbyListsCount = 16;
    private readonly List<TileIndex>[] _tileStandbyLists = new List<TileIndex>[TileStandbyListsCount];

    public override void Render(DrawingContext context)
    {
        var dirtyTiles = new Dictionary<TileIndex, SKPicture>();
        var timestamp = Stopwatch.GetTimestamp();

        var dpiScale = DpiScale();
        var visibleTiles = GetTileExtents(Bounds, Pan, Scale, dpiScale);
        var (topLeftTile, bottomRightTile) = visibleTiles;

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
                    tile.Timestamp = timestamp;

                    dirtyTiles.Add(tileIndex, recorder.EndRecording());
                }
            }
        }

        foreach (var list in _tileStandbyLists)
        {
            list.Clear();
        }

        // Trim old tiles
        var agedTiles = new List<TileIndex>();
        int agedTileCount = 0;
        foreach (var (tile, surface) in _tileMap)
        {
            if (visibleTiles.Contains(tile))
            {
                continue;
            }

            var age = Stopwatch.GetElapsedTime(surface.Timestamp, timestamp);
            var ageBucket = (uint)age.TotalMilliseconds / 256;

            if (ageBucket >= TileStandbyListsCount)
            {
                agedTiles.Add(tile);
            }
            else
            {
                _tileStandbyLists[ageBucket].Add(tile);
                agedTileCount++;
            }
        }

        for (int i = _tileStandbyLists.Length - 1; i >= 0; i--)
        {
            foreach (var tile in _tileStandbyLists[i])
            {
                if (agedTileCount >= 16)
                {
                    agedTiles.Add(tile);
                    agedTileCount--;
                }
                else
                {
                    goto removedAll;
                }
            }
        }

        removedAll:
        foreach (var tile in CollectionsMarshal.AsSpan(agedTiles))
        {
            _tileMap.Remove(tile);
        }

        context.Custom(new VirtualSurfaceRenderOperation(Bounds, Pan, Scale, dpiScale, _surfaceMap, dirtyTiles, agedTiles));
    }

    private class VirtualSurfaceRenderOperation(
        Rect renderBounds,
        Point pan,
        double scale,
        double dpiScale,
        Dictionary<TileIndex, SKSurface> surfaceMap,
        Dictionary<TileIndex, SKPicture> dirtyTiles,
        List<TileIndex> agedTiles) : ICustomDrawOperation
    {
        public Rect Bounds => renderBounds;

        public bool HitTest(Point p) => renderBounds.Contains(p);

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

            canvas.Save();

            var undoDpiScale = canvas.TotalMatrix.PostConcat(SKMatrix.CreateScale((float)dpiScale, (float)dpiScale).Invert());
            canvas.SetMatrix(undoDpiScale);

            var (panX, panY) = pan * scale * dpiScale;
            var visibleTiles = GetTileExtents(renderBounds, pan, scale, dpiScale);
            var (topLeftTile, bottomRightTile) = visibleTiles;

            for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
            {
                for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
                {
                    var tileIndex = new TileIndex(x, y);
                    ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(surfaceMap, tileIndex, out bool exists);

                    if (dirtyTiles.Remove(tileIndex, out var dirtyTilePicture))
                    {
                        var surface = tile ??= SKSurface.Create(lease.GrContext, false, new SKImageInfo(TileSide, TileSide));
                        var surfaceCanvas = surface.Canvas;

                        surfaceCanvas.Save();
                        surfaceCanvas.DrawPicture(dirtyTilePicture);
                        surfaceCanvas.Restore();

                        dirtyTilePicture.Dispose();
                    }
                    else
                    {
                        Debug.Assert(exists, "If a tile surface was just created, it must be dirty");
                    }

                    canvas.DrawSurface(tile, new SKPoint((tileIndex.X << TileSideExponent) - (float)panX, (tileIndex.Y << TileSideExponent) - (float)panY));
                }
            }

            canvas.Restore();

            Debug.Assert(dirtyTiles.Count == 0, "All dirty tiles should have been consumed");

            foreach (var tile in CollectionsMarshal.AsSpan(agedTiles))
            {
                Debug.Assert(!visibleTiles.Contains(tile), "An aged tile should never be visible");

                surfaceMap.Remove(tile, out var surface);
                Debug.Assert(surface != null, "A tile marked for deletion should not already be deleted"); // surface != null iff Remove returned true
                surface.Dispose();
            }

            agedTiles.Clear();
        }
    }
}

internal readonly struct TileSurface
{
    public SKCanvas Canvas { get; init; }

    public SKSizeI CanvasSize { get; init; }
}
