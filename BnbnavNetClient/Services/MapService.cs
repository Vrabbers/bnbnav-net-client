using BnbnavNetClient.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services;
public sealed class MapService : ReactiveObject
{

    static readonly string BaseUrl = Environment.GetEnvironmentVariable("BNBNAV_BASEURL") ?? "https://bnbnav.aircs.racing/";

    static readonly HttpClient HttpClient = new() { BaseAddress = new(BaseUrl) };

    readonly Dictionary<string, Node> _nodes;
    readonly Dictionary<string, Edge> _edges;
    readonly Dictionary<string, Road> _roads;
    readonly Dictionary<string, Landmark> _landmarks;
    readonly Dictionary<string, Annotation> _annotations;
    static string? _authenticationToken;

    public ReadOnlyDictionary<string, Node> Nodes { get; }
    public ReadOnlyDictionary<string, Edge> Edges { get; }
    public ReadOnlyDictionary<string, Road> Roads { get; }
    public ReadOnlyDictionary<string, Landmark> Landmarks { get; }

    public Interaction<Unit, string?> AuthTokenInteraction { get; } = new();

    public static string? AuthenticationToken
    {
        get => _authenticationToken;
        set
        {
            _authenticationToken = value;
            HttpClient.DefaultRequestHeaders.Authorization = value is not null ? new("Bearer", value) : null;
        }
    }

    MapService(IEnumerable<Node> nodes, IEnumerable<Edge> edges, IEnumerable<Road> roads, IEnumerable<Landmark> landmarks, IEnumerable<Annotation> annotations)
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
    }

    async Task Submit(string path, object json)
    {
        var resp = await HttpClient.PostAsync($"/api/{path}", JsonContent.Create(json, MediaTypeHeaderValue.Parse("application/json"), new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        if (resp.StatusCode == HttpStatusCode.Unauthorized && AuthTokenInteraction is not null)
        {
            AuthenticationToken = await AuthTokenInteraction.Handle(Unit.Default);
        }
    }

    async Task Delete(string path)
    {
        var resp = await HttpClient.DeleteAsync($"/api/{path}");
        if (resp.StatusCode == HttpStatusCode.Unauthorized && AuthTokenInteraction is not null)
        {
            AuthenticationToken = await AuthTokenInteraction.Handle(Unit.Default);
        }
    }

    public async Task DeleteNode(Node node)
    {
        await Delete($"/nodes/{node.Id}");
    }

    public async Task UpsertRoad(Road? road, string name, string type)
    {
        await Submit($"/roads/{road?.Id ?? "add"}", new
        {
            Name = name,
            Type = type
        });
    }

    public async Task AddEdge(Node first, Node second, Road road)
    {
        await Submit("/edges/add", new
        {
            Road = road.Id,
            Node1 = first.Id,
            Node2 = second.Id
        });
    }

    public async Task AddTwoWayEdge(Node first, Node second, Road road)
    {
        await Task.WhenAll(AddEdge(first, second, road), AddEdge(second, first, road));
    }

    public async Task DeleteEdge(Edge edge)
    {
        await Delete($"/edges/{edge.Id}");
    }

    public async Task AttachLandmark(Node node, string name, string type)
    {
        await Submit("/landmarks/add", new
        {
            Node = node.Id,
            Name = name,
            Type = type
        });
    }
    
    public async Task DetachLandmark(Landmark landmark)
    {
        await Delete($"/landmarks/{landmark.Id}");
    }

    public static async Task<MapService> DownloadInitialMapAsync()
    {
        var content = await HttpClient.GetStringAsync("/api/data");
        using var jsonDom = JsonDocument.Parse(content);

        if (jsonDom is null)
            throw new NullReferenceException(nameof(jsonDom));

        //TODO: Gracefully fail if there is no such property - this might be a new server w/o landmarks, nodes, etc.
        
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

        return new MapService(nodes.Values, edges, roads.Values, landmarks, annotations);
    }

}
