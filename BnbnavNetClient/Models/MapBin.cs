using Avalonia;
using DynamicData;

namespace BnbnavNetClient.Models;

public sealed class MapBins
{
    public class Bin
    {
        public List<Node> Nodes { get; } = [];
        public List<Edge> Edges { get; } = [];
    }
    public const int BinSideLength = 256;

    private readonly Bin?[,] _bins;
    
    private int BinsXLength => _bins.GetLength(0);
    private int BinsYLength => _bins.GetLength(1);

    public IntRect Bounds { get; private set; }
 
    public MapBins(IntRect bounds, IEnumerable<Node> nodes, IEnumerable<Edge> edges)
    {
        Bounds = bounds.Expand(5);
        var xLength = Bounds.Right - Bounds.Left;
        var yLength = Bounds.Bottom - Bounds.Top;
        var xNumBins = xLength / BinSideLength;
        var yNumBins = yLength / BinSideLength;
        _bins = new Bin?[xNumBins + 1, yNumBins + 1];

        foreach (var node in nodes)
        {
            InsertNode(node);
        }

        foreach (var edge in edges)
        {
            Insert(edge);
        }
    }
    
    public void InsertNode(Node node)
    {
        if (!Bounds.Contains(node.X, node.Z))
            throw new NotImplementedException();

        var x = node.X - Bounds.Left;
        var y = node.Z - Bounds.Top;
        var binX = x / BinSideLength;
        var binY = y / BinSideLength;

        ref var bin = ref _bins[binX, binY];
        bin ??= new Bin();
        bin.Nodes.Add(node);
    }
    
    public void Insert(Edge edge)
    {
        var minX = int.Min(edge.From.X, edge.To.X);
        var minY = int.Min(edge.From.Z, edge.To.Z);
        var maxX = int.Max(edge.From.X, edge.To.X);
        var maxY = int.Max(edge.From.Z, edge.To.Z);
        var edgeBounds = new IntRect(minX, minY, maxX, maxY);
        var expanded = edgeBounds.Expand(5);
        var startX = (expanded.Left - Bounds.Left - BinSideLength / 2) / BinSideLength;
        var startY = (expanded.Top - Bounds.Top - BinSideLength / 2) / BinSideLength;
        var endX = (expanded.Right - Bounds.Left + BinSideLength / 2) / BinSideLength;
        var endY = (expanded.Bottom - Bounds.Top + BinSideLength / 2) / BinSideLength;

        for (var i = startX; i <= endX; i++)
        {
            for (var j = startY; j <= endY; j++)
            {
                ref var bin = ref _bins[i, j];
                bin ??= new Bin();
                bin.Edges.Add(edge);
            }
        }
    }

    public void Query(IntRect rect, ref List<Node> nodes, ref List<Edge> edges)
    {
        var startX = (rect.Left - Bounds.Left - BinSideLength / 2) / BinSideLength;
        var startY = (rect.Top - Bounds.Top - BinSideLength / 2) / BinSideLength;
        var endX = (rect.Right - Bounds.Left + BinSideLength / 2) / BinSideLength;
        var endY = (rect.Bottom - Bounds.Top + BinSideLength / 2) / BinSideLength;

        for (var i = startX; i <= endX; i++)
        {
            for (var j = startY; j <= endY; j++)
            {
                ref var bin = ref _bins[i, j];
                if (bin is null)
                    continue;
                foreach (var node in bin.Nodes)
                    nodes.Add(node);
                foreach (var edge in bin.Edges)
                    edges.Add(edge);
            }
        }
    }
}