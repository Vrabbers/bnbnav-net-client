using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using SkiaSharp;

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
        public int Left => TopLeft.X;
        public int Top => TopLeft.Y;
        public int Bottom => BottomRight.Y;
        public int Right => BottomRight.X;

        public readonly bool Contains(TileIndex p)
        {
            return p.X >= TopLeft.X && p.X <= BottomRight.X
                && p.Y >= TopLeft.Y && p.Y <= BottomRight.Y;
        }
    }

    private const int TileTextureSide = 512;

    private TileRect _previousVisibleTiles;
    private readonly Dictionary<TileIndex, Tile> _tileMap = [];
    private readonly Dictionary<TileIndex, SKSurface> _surfaceMap = [];

    private long _lastRasterizationTimestamp;
    private double _lastRasterizationScale = 1;

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

    private static TileRect GetTileExtents(Size renderBounds, Point pan, double scale, double dpiScale, double renderedTileSize)
    {
        var (panX, panY) = pan * scale * dpiScale;

        var viewportRect = renderBounds * dpiScale;
        var pixelBounds = new PixelRect((int)panX, (int)panY, (int)viewportRect.Width, (int)viewportRect.Height);

        var topLeft = new TileIndex((int)double.Floor(pixelBounds.X / renderedTileSize), (int)double.Floor(pixelBounds.Y / renderedTileSize));
        var bottomRight = new TileIndex((int)double.Ceiling(pixelBounds.Right / renderedTileSize), (int)double.Ceiling(pixelBounds.Bottom / renderedTileSize));

        return new(topLeft, bottomRight);
    }

    // This function is wrong
    private static TileRect GetWorldExtends(Rect worldBounds)
    {
        //var top = (int)double.Floor(worldBounds.Bottom / TileSide);
        //var left = (int)double.Floor(worldBounds.Left / TileSide);
        //var bottom = (int)double.Ceiling(worldBounds.Bottom / TileSide);
        //var right = (int)double.Ceiling(worldBounds.Right / TileSide);

        //return new TileRect(new(left, top), new(right, bottom));
        return default;
    }

    private const int TileStandbyListsCount = 16;
    private readonly List<TileIndex>[] _tileStandbyLists = new List<TileIndex>[TileStandbyListsCount];

    public override void Render(DrawingContext context)
    {
        Dictionary<TileIndex, SKPicture>? dirtyTiles = null;
        var timestamp = Stopwatch.GetTimestamp();

        var dpiScale = DpiScale();
        var visibleTiles = GetTileExtents(Bounds.Size, Pan, Scale, dpiScale, TileTextureSide * (Scale / _lastRasterizationScale));

        bool newTilesVisible = visibleTiles.Left < _previousVisibleTiles.Left
                            || visibleTiles.Top < _previousVisibleTiles.Top
                            || visibleTiles.Right > _previousVisibleTiles.Right
                            || visibleTiles.Bottom > _previousVisibleTiles.Bottom;

        bool rerasterize = newTilesVisible || Stopwatch.GetElapsedTime(_lastRasterizationTimestamp, timestamp).TotalMilliseconds > 200;

        if (rerasterize)
        {
            _lastRasterizationScale = Scale;
            _lastRasterizationTimestamp = timestamp;
            visibleTiles = GetTileExtents(Bounds.Size, Pan, Scale, dpiScale, TileTextureSide);

            dirtyTiles = [];
            DrawDirtyTiles(dirtyTiles, timestamp, dpiScale, visibleTiles);
        }

        _previousVisibleTiles = visibleTiles;

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

        context.Custom(new VirtualSurfaceRenderOperation(Bounds, Pan, Scale, dpiScale, _lastRasterizationScale, dirtyTiles, _surfaceMap, agedTiles));
    }

    private void DrawDirtyTiles(Dictionary<TileIndex, SKPicture> dirtyTiles, long timestamp, double dpiScale, TileRect visibleTiles)
    {
        var (topLeftTile, bottomRightTile) = visibleTiles;

        for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
        {
            for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
            {
                var tileIndex = new TileIndex(x, y);

                ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(_tileMap, tileIndex, out var exists);

                if (!exists || tile.Dirty)
                {
                    using var recorder = new SKPictureRecorder();
                    var surfaceCanvas = recorder.BeginRecording(new SKRect(0, 0, TileTextureSide, TileTextureSide));

                    var worldCoordinates = new Rect(x * TileTextureSide, y * TileTextureSide, TileTextureSide, TileTextureSide) * (1 / (Scale * dpiScale));

                    surfaceCanvas.Scale((float)(Scale * dpiScale));
                    surfaceCanvas.Translate((-worldCoordinates.TopLeft).ToSKPoint());
                    DrawTile(new TileSurface { Canvas = surfaceCanvas, CanvasSize = new(TileTextureSide, TileTextureSide) }, worldCoordinates);

                    tile.Dirty = false;
                    tile.Timestamp = timestamp;

                    dirtyTiles.Add(tileIndex, recorder.EndRecording());
                }
            }
        }
    }

    private class VirtualSurfaceRenderOperation(
        Rect renderBounds,
        Point pan,
        double scale,
        double dpiScale,
        double lastRasterizationScale,
        Dictionary<TileIndex, SKPicture>? dirtyTiles,
        Dictionary<TileIndex, SKSurface> surfaceMap,
        List<TileIndex> agedTiles) : ICustomDrawOperation
    {
        public Rect Bounds => renderBounds;

        public bool HitTest(Point p) => renderBounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => this == other;

        public void Dispose()
        {
        }

        private static readonly SKPaint HighQualityFilter = new()
        {
            FilterQuality = SKFilterQuality.High
        };

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

            bool rerasterize = dirtyTiles != null;

            float comp = rerasterize ? 1f : (float)(scale / lastRasterizationScale);

            var (panX, panY) = pan * scale * dpiScale;
            var visibleTiles = GetTileExtents(renderBounds.Size, pan, scale, dpiScale, TileTextureSide * comp);
            var (topLeftTile, bottomRightTile) = visibleTiles;

            var timestamp = Stopwatch.GetTimestamp();

            if (!rerasterize)
            {
                var scaleInterpolate = canvas.TotalMatrix.PreConcat(SKMatrix.CreateScale(comp, comp, (float)-panX, (float)-panY));
                canvas.SetMatrix(scaleInterpolate);
            }

            for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
            {
                for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
                {
                    var tileIndex = new TileIndex(x, y);
                    ref var tile = ref CollectionsMarshal.GetValueRefOrAddDefault(surfaceMap, tileIndex, out bool existed);

                    if (rerasterize)
                    {
                        if (dirtyTiles!.Remove(tileIndex, out var dirtyTilePicture))
                        {
                            var surface = tile ??= SKSurface.Create(lease.GrContext, false, new SKImageInfo(TileTextureSide, TileTextureSide));
                            var surfaceCanvas = surface.Canvas;

                            surfaceCanvas.Save();
                            surfaceCanvas.DrawPicture(dirtyTilePicture);
                            surfaceCanvas.Restore();

                            dirtyTilePicture.Dispose();
                        }
                        else
                        {
                            Debug.Assert(existed, "If a tile surface was just created, it must be dirty");
                        }
                    }

                    canvas.DrawSurface(tile, new SKPoint((tileIndex.X * TileTextureSide) - (float)panX, (tileIndex.Y * TileTextureSide) - (float)panY), HighQualityFilter);
                }
            }

            canvas.Restore();

            if (rerasterize)
            {
                Debug.Assert(dirtyTiles!.Count == 0, "All dirty tiles should have been consumed");
            }

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
