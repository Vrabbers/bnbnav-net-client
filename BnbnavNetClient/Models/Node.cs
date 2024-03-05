using Avalonia;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public class Node(string id, int x, int y, int z, string world)
    : MapItem, ILocatable
{
    static readonly double NodeSize = 14;

    public string Id { get; init; } = id;
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public int Z { get; set; } = z;
    public string World { get; set; } = world;

    public Rect BoundingRect(MapView mapView)
    {
        var pos = mapView.ToScreen(new Point(X, Z));
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
    public TemporaryNode(int x, int y, int z, string world) : base($"temp@{x},{z}", x, y, z, world)
    {
    }

    public TemporaryNode(ISearchable original) : base(
        $"temp@{original.Location.X},{original.Location.Z}:{original.Name}", original.Location.X, original.Location.Y,
        original.Location.Z, original.Location.World)
    {
        OriginalSearchable = original;
    }
    
    ISearchable? OriginalSearchable { get; }
}