using Avalonia;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public class Node : MapItem, ILocatable
{
    static readonly double NodeSize = 14;

    public Node(string Id, int X, int Y, int Z)
    {
        this.Id = Id;
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

    public string Id { get; init; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public Rect BoundingRect(MapView mapView)
    {
        var pos = mapView.ToScreen(new(X, Z));
        var rect = new Rect(
            pos.X - NodeSize / 2, 
            pos.Y - NodeSize / 2,
            NodeSize, NodeSize);
        return rect;
    }

    public void Deconstruct(out string id, out int x, out int y, out int z)
    {
        id = Id;
        x = X;
        y = Y;
        z = Z;
    }

    public Point Point => new(X, Z);
}

public class TemporaryNode : Node
{
    public TemporaryNode(int X, int Y, int Z) : base($"temp@{X},{Z}", X, Y, Z)
    {
    }

    public TemporaryNode(ISearchable original) : base(
        $"temp@{original.Location.X},{original.Location.Z}:{original.Name}", original.Location.X, original.Location.Y,
        original.Location.Z)
    {
        OriginalSearchable = original;
    }
    
    ISearchable? OriginalSearchable { get; }
}