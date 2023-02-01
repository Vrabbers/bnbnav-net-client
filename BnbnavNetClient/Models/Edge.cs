using Avalonia;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Models;

public class Edge : MapItem
{
    public Edge(string Id, Road Road, Node From, Node To)
    {
        this.Id = Id;
        this.Road = Road;
        this.From = From;
        this.To = To;
    }

    public (Point, Point) Extents(MapView mapView)
    {
        var from = mapView.ToScreen(From.Point);
        var to = mapView.ToScreen(To.Point);
        return (from, to);
    }

    public string Id { get; init; }
    public Road Road { get; init; }
    public Node From { get; init; }
    public Node To { get; init; }

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

public class TemporaryEdge : Edge
{
    public TemporaryEdge(Road Road, Node From, Node To) : base("somegeneratedid", Road, From, To)
    {
    }
}