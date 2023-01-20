using Avalonia;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public sealed record class Node(string Id, int X, int Y, int Z) : MapItem
{
    static readonly double NodeSize = 14;

    public Rect BoundingRect(MapView mapView)
    {
        var pos = mapView.ToScreen(new(this.X, this.Z));
        var rect = new Rect(
            pos.X - NodeSize / 2, 
            pos.Y - NodeSize / 2,
            NodeSize, NodeSize);
        return rect;
    }
}
