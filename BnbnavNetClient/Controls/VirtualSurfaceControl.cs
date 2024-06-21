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
    private struct Tile { public SKSurface Surface; public bool Dirty; public long Timestamp; }
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

    public double Scale { get; set; } = 1;
    public Point Pan { get; set; }

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
    
    public override void Render(DrawingContext context)
    {
        var dpiScale = DpiScale();
        var (topLeftTile, bottomRightTile) = GetTileExtents(Bounds, Pan, Scale, dpiScale);

        var dirtyTiles = new Dictionary<TileIndex, SKPicture>();

        var timestamp = Stopwatch.GetTimestamp();

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

            var (panX, panY) = pan * scale * dpiScale;
            var visibleTiles = GetTileExtents(Bounds, pan, scale, dpiScale);

            canvas.Save();

            var undoDpiScale = canvas.TotalMatrix.PostConcat(SKMatrix.CreateScale((float)dpiScale, (float)dpiScale).Invert());
            canvas.SetMatrix(undoDpiScale);

            var timestamp = Stopwatch.GetTimestamp();

            var (topLeftTile, bottomRightTile) = visibleTiles;
            for (var x = topLeftTile.X; x <= bottomRightTile.X; x++)
            {
                for (var y = topLeftTile.Y; y <= bottomRightTile.Y; y++)
                {
                    var tileIndex = new TileIndex(x, y);
                    ref var tile = ref CollectionsMarshal.GetValueRefOrNullRef(tileMap, tileIndex);

                    if (dirtyTiles.TryGetValue(tileIndex, out var dirtyTilePicture))
                    {
                        var surface = tile.Surface ??= SKSurface.Create(lease.GrContext, false, new SKImageInfo(TileSide, TileSide));
                        var surfaceCanvas = surface.Canvas;

                        surfaceCanvas.Save();
                        surfaceCanvas.DrawPicture(dirtyTilePicture);
                        surfaceCanvas.Restore();

                        dirtyTilePicture.Dispose();
                    }

                    tile.Timestamp = timestamp;
                    canvas.DrawSurface(tile.Surface, new SKPoint((tileIndex.X << TileSideExponent) - (float)panX, (tileIndex.Y << TileSideExponent) - (float)panY));
                }
            }

            canvas.Restore();

            //var tilesToFree = new List<(TileIndex, SKSurface)>();
            //var tilesStandbyList = new SortedDictionary<TimeSpan, (TileIndex, SKSurface)>(
            //    Comparer<TimeSpan>.Create((l, r) => -l.CompareTo(r)));

            //foreach (var (coord, surface) in tileMap)
            //{
            //    if (visibleTiles.Contains(coord))
            //    {
            //        continue;
            //    }

            //    var age = Stopwatch.GetElapsedTime(timestamp);
            //    if (age.TotalSeconds > 2)
            //    {
            //        tilesToFree.Add((coord, surface.Surface));
            //    }
            //    else
            //    {
            //        tilesStandbyList.Add(age, (coord, surface.Surface));
            //    }
            //}

            //foreach (var (coord, surface) in tilesToFree)
            //{
            //    surface.Dispose();
            //    tileMap.Remove(coord);
            //}

            
            //while (tilesStandbyList.Count > 16)
            //{
            //    var (coord, surface) = tilesStandbyList.Values.First();
            //    surface.Dispose();
            //    tileMap.Remove(coord);
            //}
        }
    }
}

internal readonly struct TileSurface
{
    public SKCanvas Canvas { get; init; }

    public SKSizeI CanvasSize { get; init; }
}
