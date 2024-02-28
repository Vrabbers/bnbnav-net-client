using Avalonia;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public class Edge(string id, Road road, Node from, Node to)
    : MapItem
{
    public (Point, Point) Extents(MapView mapView)
    {
        var from = mapView.ToScreen(From.Point);
        var to = mapView.ToScreen(To.Point);
        return (from, to);
    }

    public string Id { get; init; } = id;
    public Road Road { get; init; } = road;
    public Node From { get; init; } = from;
    public Node To { get; init; } = to;

    public void Deconstruct(out string id, out Road road, out Node from, out Node to)
    {
        id = Id;
        road = Road;
        from = From;
        to = To;
    }

    public bool CanSnapTo => Road.RoadType != RoadType.DuongWarp;

    public ExtendedLine Line => new()
    {
        Point1 = From.Point,
        Point2 = To.Point
    };
}

public class TemporaryEdge(Road road, Node from, Node to) : Edge("somegeneratedid", road, from, to);