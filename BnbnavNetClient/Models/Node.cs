using Avalonia;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public sealed class Node : MapItem
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
        var pos = mapView.ToScreen(new(this.X, this.Z));
        var rect = new Rect(
            pos.X - NodeSize / 2, 
            pos.Y - NodeSize / 2,
            NodeSize, NodeSize);
        return rect;
    }

    public void Deconstruct(out string Id, out int X, out int Y, out int Z)
    {
        Id = this.Id;
        X = this.X;
        Y = this.Y;
        Z = this.Z;
    }

    public Point Point => new(X, Z);
}
