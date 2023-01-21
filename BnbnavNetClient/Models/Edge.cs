using Avalonia;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public sealed record Edge(string Id, Road Road, Node From, Node To) : MapItem
{
    public (Point, Point) Extents(MapView mapView)
    {
        var from = mapView.ToScreen(new(From.X, From.Z));
        var to = mapView.ToScreen(new (To.X, To.Z));
        return (from, to);
    }
}
