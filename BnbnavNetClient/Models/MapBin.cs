using CommunityToolkit.HighPerformance;
using System.Collections;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace BnbnavNetClient.Models;

public sealed class MapBin
{
    public class Bin
    {
        public List<Node> Nodes { get; } = [];

        public List<IntRect> EdgeRects { get; } = [];
        public List<Edge> Edges { get; } = [];
        public required IntRect Bounds { get; init; }
        
        public BinAttachedRenderTarget? RenderTarget { get; set; } 
    }
    public const int BinSideLength = 1024;

    private readonly Bin?[,] _bins;
    
    private int BinsXLength => _bins.GetLength(1);
    private int BinsYLength => _bins.GetLength(0);

    public IntRect Bounds { get; private set; }
 
    public MapBin(IntRect bounds, IEnumerable<Node> nodes, IEnumerable<Edge> edges)
    {
        Bounds = bounds.Expand(5);
        var xLength = Bounds.Right - Bounds.Left;
        var yLength = Bounds.Bottom - Bounds.Top;
        var xNumBins = xLength / BinSideLength;
        var yNumBins = yLength / BinSideLength;
        _bins = new Bin?[yNumBins + 1, xNumBins + 1];

        foreach (var node in nodes)
        {
            InsertNode(node);
        }

        foreach (var edge in edges)
        {
            InsertEdge(edge);
        }
    }

    private IntRect BoundsForBin(int x, int y)
    {
        var left = Bounds.Left + BinSideLength * x;
        var right = left + BinSideLength;
        var top = Bounds.Top + BinSideLength * y;
        var bottom = top + BinSideLength;
        return new IntRect(left, top, right, bottom);
    }
    
    public void InsertNode(Node node)
    {
        if (!Bounds.Contains(node.X, node.Z))
            throw new NotImplementedException();
        
        var x = node.X - Bounds.Left;
        var y = node.Z - Bounds.Top;
        var binX = x / BinSideLength;
        var binY = y / BinSideLength;

        ref var bin = ref _bins[binY, binX];
        bin ??= new Bin { Bounds = BoundsForBin(binX, binY) };
        bin.Nodes.Add(node);
    }
    
    public void InsertEdge(Edge edge)
    {
        var minX = int.Min(edge.From.X, edge.To.X);
        var minY = int.Min(edge.From.Z, edge.To.Z);
        var maxX = int.Max(edge.From.X, edge.To.X);
        var maxY = int.Max(edge.From.Z, edge.To.Z);
        var edgeBounds = new IntRect(minX, minY, maxX, maxY);
        var expanded = edgeBounds.Expand(5);
        var startX = (expanded.Left - Bounds.Left) / BinSideLength;
        var startY = (expanded.Top - Bounds.Top) / BinSideLength;
        var endX = (expanded.Right - Bounds.Left) / BinSideLength;
        var endY = (expanded.Bottom - Bounds.Top) / BinSideLength;

        for (var j = startY; j <= endY; j++)
        {
            for (var i = startX; i <= endX; i++)
            {
                ref var bin = ref _bins[j, i];
                bin ??= new Bin { Bounds = BoundsForBin(i, j) };
                bin.Edges.Add(edge);
                bin.EdgeRects.Add(expanded);
            }
        }
    }

    public Span2D<Bin?> QueryBins(IntRect queryRect)
    {
        queryRect = queryRect.Intersect(Bounds);
        var startX = (queryRect.Left - Bounds.Left) / BinSideLength;
        var startY = (queryRect.Top - Bounds.Top ) / BinSideLength;
        var endX = (queryRect.Right - Bounds.Left) / BinSideLength;
        var endY = (queryRect.Bottom - Bounds.Top) / BinSideLength;
        return new Span2D<Bin?>(_bins, startY, startX, endY - startY + 1, endX - startX + 1);
    }
    
    public void QueryDrawn(IntRect queryRect, List<Node> nodes, List<Edge> edges)
    {
        queryRect = queryRect.Intersect(Bounds);
        var startX = (queryRect.Left - Bounds.Left) / BinSideLength;
        var startY = (queryRect.Top - Bounds.Top) / BinSideLength;
        var endX = (queryRect.Right - Bounds.Left) / BinSideLength;
        var endY = (queryRect.Bottom - Bounds.Top) / BinSideLength;

        for (var j = startY; j <= endY; j++)
        {
            for (var i = startX; i <= endX; i++)
            {
                var bin = _bins[j, i];
                if (bin is null)
                    continue;

                var binRect = bin.Bounds;
                
                foreach (var node in bin.Nodes)
                    nodes.Add(node);

                for (var k = 0; k < bin.Edges.Count; k++)
                {
                    var (top, left) = bin.EdgeRects[k].IntersectTopLeft(queryRect);
                    if (binRect.Contains(top, left))
                        edges.Add(bin.Edges[k]);
                }
            }
        }
    }
}

public record BinAttachedRenderTarget(RenderTargetBitmap Bitmap);