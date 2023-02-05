using BnbnavNetClient.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BnbnavNetClient.Services.NetworkOperations;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.Services;

public sealed class MapService : ReactiveObject
{
    public class ServerResponse
    {
        public required HttpStatusCode StatusCode { get; init; }
        public required Stream Stream { get; init; }

        public ServerResponse AssertSuccess()
        {
            if (StatusCode is < (HttpStatusCode) 200 or > (HttpStatusCode) 299)
            {
                throw new NetworkOperationException($"Server returned {StatusCode}");
            }

            return this;
        }
    }

    [Flags]
    public enum RouteOptions
    {
        NoRouteOptions = 0,
        AvoidMotorways = 1,
        AvoidDuongWarp = 2,
        AvoidTolls = 4,
        AvoidFerries = 8
    }

    public static readonly string BaseUrl = Environment.GetEnvironmentVariable("BNBNAV_BASEURL") ?? "https://bnbnav.aircs.racing/";

    static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri(BaseUrl),
        DefaultRequestHeaders =
        {
            UserAgent =
            {
                new ProductInfoHeaderValue("bnbnav-dotnet", "1.0")                
            }
        }
    };

    readonly Dictionary<string, Node> _nodes;
    readonly Dictionary<string, Edge> _edges;
    readonly Dictionary<string, Road> _roads;
    readonly Dictionary<string, Landmark> _landmarks;
    readonly Dictionary<string, Annotation> _annotations;
    readonly Dictionary<string, Player> _players;
    static string? _authenticationToken;

    readonly BnbnavWebsocketService _websocketService;

    public ReadOnlyDictionary<string, Node> Nodes { get; }
    public ReadOnlyDictionary<string, Edge> Edges { get; }
    public ReadOnlyDictionary<string, Road> Roads { get; }
    public ReadOnlyDictionary<string, Landmark> Landmarks { get; }
    public ReadOnlyDictionary<string, Player> Players { get; }
    List<(string, object?, TaskCompletionSource<ServerResponse>)> PendingRequests { get; } = new();
    public Interaction<Unit, string?> AuthTokenInteraction { get; } = new();
    public Interaction<Unit, Unit> PlayerUpdateInteraction { get; } = new();
    
    [Reactive]
    public CalculatedRoute? CurrentRoute { get; set; }
    
    [ObservableAsProperty]
    public string? LoggedInUsername { get; set; }

    [Reactive]
    public Player? LoggedInPlayer { get; set; }

    public IEnumerable<Edge> AllEdges =>
        (CurrentRoute?.Edges ?? Enumerable.Empty<Edge>()).Union(Edges.Values).Distinct().Reverse();

    public static string? AuthenticationToken
    {
        get => _authenticationToken;
        set
        {
            _authenticationToken = value;
            HttpClient.DefaultRequestHeaders.Authorization = value is not null ? new AuthenticationHeaderValue("Bearer", value) : null;
        }
    }

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
        _players = new Dictionary<string, Player>();
        Players = _players.AsReadOnly();
        _websocketService = websocketService;

        this.WhenAnyValue(x => x.LoggedInUsername).Subscribe(Observer.Create<string?>(_ => UpdateLoggedInPlayer()));
        this.WhenPropertyChanged(x => x.Players)
            .Subscribe(Observer.Create<PropertyValue<MapService, ReadOnlyDictionary<string, Player>>>(_ =>
                UpdateLoggedInPlayer()));
    }

    void UpdateLoggedInPlayer()
    {
        if (LoggedInUsername is null)
        {
            LoggedInPlayer = null;
            return;
        }

        LoggedInPlayer = Players.TryGetValue(LoggedInUsername, out var player) ? player : null;
    }

    public Edge? OppositeEdge(Edge edge, IEnumerable<Edge> list)
    {
        return list.SingleOrDefault(x => x.To == edge.From && x.From == edge.To);
    }

    public Edge? OppositeEdge(Edge edge)
    {
        return OppositeEdge(edge, _edges.Values);
    }

    public async Task<ServerResponse> Submit(string path, object json)
    {
        var resp = await HttpClient.PostAsync($"/api/{path}", JsonContent.Create(json, MediaTypeHeaderValue.Parse("application/json"), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await HandleUnauthorizedResponse(path, json);
        }

        return new ServerResponse
        {
            StatusCode = resp.StatusCode,
            Stream = await resp.Content.ReadAsStreamAsync()
        };
    }

    async Task<ServerResponse> HandleUnauthorizedResponse(string path, object? json)
    {
        var completionSource = new TaskCompletionSource<ServerResponse>();
        var showDialog = !PendingRequests.Any();
        PendingRequests.Add((path, json, completionSource));
        
        if (showDialog)
        {
            try
            {
                //Request a new auth token from the user and then replay pending requests
                AuthenticationToken = await AuthTokenInteraction.Handle(Unit.Default);
                if (AuthenticationToken is null)
                {
                    CancelPendingRequests(new NetworkOperationException("Authentication token not provided"));
                }
                else
                {
                    ReplayPendingRequests();
                }
            }
            catch (Exception e)
            {
                //Reject all the pending requests as an exception has occurred
                CancelPendingRequests(e);
            }
        }

        return await completionSource.Task;
    }

    public async Task<ServerResponse> Delete(string path)
    {
        var resp = await HttpClient.DeleteAsync($"/api/{path}");
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await HandleUnauthorizedResponse(path, null);
        }
        
        return new ServerResponse
        {
            StatusCode = resp.StatusCode,
            Stream = await resp.Content.ReadAsStreamAsync()
        };
    }

    void CancelPendingRequests(Exception e)
    {
        var requests = PendingRequests.ToList();
        PendingRequests.Clear();
        foreach (var (_, _, completionSource) in requests)
        {
            completionSource.SetException(e);
        }
    }

    async void ReplayPendingRequests()
    {
        var requests = PendingRequests.ToList();
        PendingRequests.Clear();
        await Task.WhenAll(requests.Select(async x=>
        {
            var (path, payload, completionSource) = x;
            if (payload is null)
            {
                completionSource.SetResult(await Delete(path));
            }
            else
            {
                completionSource.SetResult(await Submit(path, payload));
            }
        }));
    }

    public async Task<CalculatedRoute> ObtainCalculatedRoute(ISearchable from, ISearchable to, RouteOptions routeOptions, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            var edges = _edges.Values
                .Where(x => !routeOptions.HasFlag(RouteOptions.AvoidMotorways) || x.Road.RoadType != RoadType.Motorway)
                .Where(x => !routeOptions.HasFlag(RouteOptions.AvoidDuongWarp) || x.Road.RoadType != RoadType.DuongWarp)
                .ToList();
            
            var point1Edge = edges.MinBy(x => x.Line.RightAngleIntersection(from.Location.Point, out var intersection) ? intersection.Length : int.MaxValue);
            var point2Edge = edges.MinBy(x => x.Line.RightAngleIntersection(to.Location.Point, out var intersection) ? intersection.Length : int.MaxValue);

            ct.ThrowIfCancellationRequested();
            if (point1Edge is null || point2Edge is null) throw new NoSuitableEdgeException();

            var startingNode = new TemporaryNode(from);
            var endingNode = new TemporaryNode(to);

            IEnumerable<Edge> GenerateTemporaryEdgesFromPointToEdge(Node point, Edge edge, bool pointToEdge)
            {
                _ = edge.Line.RightAngleIntersection(point.Point, out var normalLine);
                var tempNode = new TemporaryNode((int)normalLine.Point1.X, 0, (int)normalLine.Point1.Y);

                var isTwoWay = OppositeEdge(edge, edges) is not null;
                if (pointToEdge)
                {
                    yield return new TemporaryEdge(edge.Road, point, tempNode);
                    yield return new TemporaryEdge(edge.Road, tempNode, edge.To);
                    if (isTwoWay) yield return new TemporaryEdge(edge.Road, tempNode, edge.From);
                }
                else
                {
                    yield return new TemporaryEdge(edge.Road, tempNode, point);
                    yield return new TemporaryEdge(edge.Road, edge.From, tempNode);
                    if (isTwoWay) yield return new TemporaryEdge(edge.Road, edge.To, tempNode);
                }
            }

            //Construct temporary edges from the starting node
            if (from is Player { SnappedEdge: { } } player)
            {
                //Connect the starting node to the map using the existing road
                edges.Add(new TemporaryEdge(player.SnappedEdge.Road, startingNode, player.SnappedEdge.To));
            }
            else
            {
                //Connect the starting node to the map using two temporary edges (three for a bidirectional road)
                edges.AddRange(GenerateTemporaryEdgesFromPointToEdge(startingNode, point1Edge, true));
            }
            
            //Construct temporary edges to the ending node
            edges.AddRange(GenerateTemporaryEdgesFromPointToEdge(endingNode, point2Edge, false));
            
            //Execute the shortest path algorithm to locate the shortest path between the starting node and the ending node
            var queue = new Dictionary<Node, (Node node, int distance, Edge? via)> { { startingNode, (startingNode, 0, null) } };
            var backtrack = new List<(Node node, int distance, Edge? via)>();
            while (queue.Any())
            {
                ct.ThrowIfCancellationRequested();
                var processingNode = queue.MinBy(x => x.Value.distance).Value;
                queue.Remove(processingNode.node);
                backtrack.Add(processingNode);

                if (processingNode.node == endingNode)
                {
                    //We have reached the end! Start backtracking!
                    var route = new CalculatedRoute(this);
                    do
                    {
                        ct.ThrowIfCancellationRequested();
                        route.AddRouteSegment(processingNode.node, processingNode.via);
                        processingNode = backtrack.Single(x => x.node == processingNode.via!.From);
                    } while (processingNode.via is not null);
                    route.AddRouteSegment(processingNode.node, null);
                    return route;
                }

                //Find each edge that this node connects to
                //TODO: Turn restrictions should be calculated here
                var fromEdges = edges.Where(x => x.From == processingNode.node && backtrack.All(y => y.node != x.To));
                foreach (var edge in fromEdges)
                {
                    var oldDistance = queue.TryGetValue(edge.To, out var node) ? node.distance : int.MaxValue;
                    var newDistance = (int) (processingNode.distance + edge.Line.Length * edge.Road.RoadType.RoadPenalty());
                    if (newDistance < oldDistance)
                    {
                        queue[edge.To] = (edge.To, newDistance, edge);
                    }
                }
            }

            //No route exists
            throw new DisjointNetworkException();
        }, ct);
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
                    _nodes.Add(id, new Node(id, node.X, node.Y, node.Z));
                    break;

                case UpdatedNode node:
                    type = nameof(Nodes);
                    _nodes[id].X = node.X;
                    _nodes[id].Y = node.Y;
                    _nodes[id].Z = node.Z;
                    break;

                case NodeRemoved:
                    type = nameof(Nodes);
                    _nodes.Remove(id);
                    break;

                case RoadCreated road:
                    type = nameof(Roads);
                    _roads.Add(id, new Road(id, road.Name, road.RoadType));
                    break;

                case UpdatedRoad road:
                    type = nameof(Roads);
                    _roads[id].Name = road.Name;
                    _roads[id].Type = road.RoadType;
                    break;

                case RoadRemoved:
                    type = nameof(Roads);
                    _roads.Remove(id);
                    break;

                case EdgeCreated edge:
                    type = nameof(Edges);
                    _edges.Add(id, new Edge(id, _roads[edge.Road], _nodes[edge.Node1], _nodes[edge.Node2]));
                    break;

                case EdgeRemoved:
                    type = nameof(Edges);
                    _edges.Remove(id);
                    break;

                case LandmarkCreated landmark:
                    type = nameof(Landmarks);
                    _landmarks.Add(id, new Landmark(id, _nodes[landmark.Node], landmark.Name, landmark.LandmarkType));
                    break;

                case LandmarkRemoved:
                    type = nameof(Landmarks);
                    _landmarks.Remove(id);
                    break;
                
                case PlayerMoved player:
                    type = nameof(Players);
                    if (_players.ContainsKey(player.Id!))
                    {
                        _players[player.Id!].HandlePlayerMovedEvent(player);
                    }
                    else
                    {
                        var p = new Player(player.Id!, this);
                        p.PlayerUpdateEvent += (_, _) =>
                        {
                            PlayerUpdateInteraction.Handle(Unit.Default);
                        };
                        p.HandlePlayerMovedEvent(player);
                        _players.Add(player.Id!, p);
                    }

                    break;
                case PlayerLeft player:
                    type = nameof(Players);
                    if (_players.ContainsKey(player.Id!))
                    {
                        _players[player.Id!].HandlePlayerGoneEvent();
                        _players.Remove(player.Id!);
                    }
                    break;
            }

            this.RaisePropertyChanged(type);
        }
    }
}
