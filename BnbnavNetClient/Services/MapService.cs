using BnbnavNetClient.Models;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services;

public sealed class MapService : ReactiveObject
{
    public static readonly string BaseUrl = Environment.GetEnvironmentVariable("BNBNAV_BASEURL") ?? "https://bnbnav.aircs.racing/";

    static readonly HttpClient HttpClient = new() { BaseAddress = new(BaseUrl) };

    readonly Dictionary<string, Node> _nodes;
    readonly Dictionary<string, Edge> _edges;
    readonly Dictionary<string, Road> _roads;
    readonly Dictionary<string, Landmark> _landmarks;
    readonly Dictionary<string, Annotation> _annotations;

    readonly BnbnavWebsocketService _websocketService;

    public ReadOnlyDictionary<string, Node> Nodes { get; }
    public ReadOnlyDictionary<string, Edge> Edges { get; }
    public ReadOnlyDictionary<string, Road> Roads { get; }
    public ReadOnlyDictionary<string, Landmark> Landmarks { get; }

    MapService(IEnumerable<Node> nodes, IEnumerable<Edge> edges, IEnumerable<Road> roads, IEnumerable<Landmark> landmarks, IEnumerable<Annotation> annotations, BnbnavWebsocketService websocketService)
    {

        _nodes = new Dictionary<string, Node>(nodes.ToDictionary(n => n.Id));
        Nodes = _nodes.AsReadOnly();
        _edges = new Dictionary<string, Edge>(edges.ToDictionary(e => e.Id));
        Edges = _edges.AsReadOnly();
        _roads = new Dictionary<string, Road>(roads.ToDictionary(r => r.Id));
        Roads = _roads.AsReadOnly();
        _landmarks = new Dictionary<string, Landmark>(landmarks.ToDictionary(l => l.Id));
        Landmarks = _landmarks.AsReadOnly();
        _annotations = new Dictionary<string, Annotation>(annotations.ToDictionary(a => a.Id));
        _websocketService = websocketService;
    }

    public static async Task<MapService> DownloadInitialMapAsync()
    {
        var content = await HttpClient.GetStringAsync("/api/data");
        using var jsonDom = JsonDocument.Parse(content);

        if (jsonDom is null)
            throw new NullReferenceException(nameof(jsonDom));

        var root = jsonDom.RootElement;
        var jsonNodes = root.GetProperty("nodes"u8);
        var nodes = new Dictionary<string, Node>();
        foreach (var jsonNode in jsonNodes.EnumerateObject())
        {
            var id = jsonNode.Name;
            var obj = jsonNode.Value;
            var x = obj.GetProperty("x"u8).GetInt32();
            var y = obj.GetProperty("y"u8).GetInt32();
            var z = obj.GetProperty("z"u8).GetInt32();
            nodes.Add(id, new Node(id, x, y, z));
        }

        var jsonLandmarks = root.GetProperty("landmarks"u8);
        var landmarks = new List<Landmark>();
        foreach (var jsonLandmark in jsonLandmarks.EnumerateObject())
        {
            var id = jsonLandmark.Name;
            var obj = jsonLandmark.Value;
            var name = obj.GetProperty("name"u8).GetString()!;
            var type = obj.GetProperty("type"u8).GetString()!;
            var node = nodes[obj.GetProperty("node"u8).GetString()!];
            landmarks.Add(new Landmark(id, node, name, type));
        }

        var jsonRoads = root.GetProperty("roads"u8);
        var roads = new Dictionary<string, Road>();
        foreach (var jsonRoad in jsonRoads.EnumerateObject())
        {
            var id = jsonRoad.Name;
            var obj = jsonRoad.Value;
            var name = obj.GetProperty("name"u8).GetString()!;
            var type = obj.GetProperty("type"u8).GetString()!;
            roads.Add(id, new Road(id, name, type));
        }

        var jsonEdges = root.GetProperty("edges"u8);
        var edges = new List<Edge>();
        foreach (var jsonEdge in jsonEdges.EnumerateObject())
        {
            var id = jsonEdge.Name;
            var obj = jsonEdge.Value;
            var road = roads[obj.GetProperty("road"u8).GetString()!];
            var node1 = nodes[obj.GetProperty("node1"u8).GetString()!];
            var node2 = nodes[obj.GetProperty("node2"u8).GetString()!];
            edges.Add(new Edge(id, road, node1, node2));
        }

        var jsonAnnotations = root.GetProperty("annotations"u8);
        var annotations = new List<Annotation>();
        foreach (var jsonAnnotation in jsonAnnotations.EnumerateObject())
        {
            var id = jsonAnnotation.Name;
            var obj = jsonAnnotation.Value;
            annotations.Add(new Annotation(id, obj.Clone()));
        }

        var ws = new BnbnavWebsocketService();
        await ws.ConnectAsync(CancellationToken.None);
        var service = new MapService(nodes.Values, edges, roads.Values, landmarks, annotations, ws);
        _ = service.ProcessChangesAsync();
        return service;
    }

    async Task ProcessChangesAsync()
    {
        await foreach (var message in _websocketService.GetMessages(CancellationToken.None))
        {
            string id = message.Id!;
            string type = "";
            switch (message)
            {
                case NodeCreated node:
                    type = nameof(Nodes);
                    _nodes.Add(id, new(id, node.X, node.Y, node.Z));
                    break;

                case UpdatedNode node:
                    type = nameof(Nodes);
                    _nodes[id] = new(id, node.X, node.Y, node.Z);
                    break;

                case NodeRemoved:
                    type = nameof(Nodes);
                    _nodes.Remove(id);
                    break;

                case RoadCreated road:
                    type = nameof(Roads);
                    _roads.Add(id, new(id, road.Name, road.RoadType));
                    break;

                case UpdatedRoad road:
                    type = nameof(Roads);
                    _roads[id] = new(id, road.Name, road.RoadType);
                    break;

                case RoadRemoved:
                    type = nameof(Roads);
                    _roads.Remove(id);
                    break;

                case EdgeCreated edge:
                    type = nameof(Edges);
                    _edges.Add(id, new(id, _roads[edge.Road], _nodes[edge.Node1], _nodes[edge.Node2]));
                    break;

                case EdgeRemoved:
                    type = nameof(Edges);
                    _edges.Remove(id);
                    break;

                case LandmarkCreated landmark:
                    type = nameof(Landmarks);
                    _landmarks.Add(id, new(id, _nodes[landmark.Node], landmark.Name, landmark.LandmarkType));
                    break;

                case LandmarkRemoved:
                    type = nameof(Landmarks);
                    _landmarks.Remove(id);
                    break;
            }

            this.RaisePropertyChanged(type);
        }
    }
}
