using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services;

internal sealed class BnbnavWebsocketService
{
    private ClientWebSocket _ws = null!;


    public async Task ConnectAsync(CancellationToken ct)
    {
        while (true)
        {
            try
            {
                _ws = new();

                var uri = new UriBuilder(MapService.BaseUrl)
                {
                    Scheme = new Uri(MapService.BaseUrl).Scheme switch
                    {
                        "http" => Uri.UriSchemeWs,
                        "https" => Uri.UriSchemeWss,
                        _ => throw new ArgumentException()
                    }
                };
              
                await _ws.ConnectAsync(uri.Uri, ct);
                return;
            }
            catch (WebSocketException)
            {
                await Task.Delay(5000, ct);
            }
        }
    }

    public async Task<ReadOnlyMemory<byte>> NextMessageAsync(CancellationToken ct)
    {
        var writer = new ArrayBufferWriter<byte>();

        bool finished;
        do
        {
            var mem = writer.GetMemory();
            var result = await _ws.ReceiveAsync(mem, ct);
            writer.Advance(result.Count);
            finished = result.EndOfMessage;
        } while (!finished);

        return writer.WrittenMemory;
    }

}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(NewNode), "newNode")]
[JsonDerivedType(typeof(NewRoad), "newRoad")]
[JsonDerivedType(typeof(NewEdge), "newEdge")]
[JsonDerivedType(typeof(NewLandmark), "newLandmark")]
[JsonDerivedType(typeof(EdgeRemoved), "edgeRemoved")]
[JsonDerivedType(typeof(RoadRemoved), "roadRemoved")]
[JsonDerivedType(typeof(NodeRemoved), "nodeRemoved")]
[JsonDerivedType(typeof(LandmarkRemoved), "landmarkRemoved")]
[JsonDerivedType(typeof(UpdatedNode), "nodeUpdated")]
[JsonDerivedType(typeof(UpdatedRoad), "roadUpdated")]
[JsonDerivedType(typeof(UpdatedAnnotation), "annotationUpdated")]
[JsonDerivedType(typeof(RemovedAnnotation), "annotationRemoved")]
[JsonDerivedType(typeof(PlayerMoved), "playerMove")]
[JsonDerivedType(typeof(PlayerLeft), "playerGone")]
internal record BnbnavMessage
{
    public string? Id { get; init; }
}

internal record Node : BnbnavMessage
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Z { get; init; }

    public required string Player { get; init; }
}

internal sealed record NewNode : Node;
internal sealed record UpdatedNode : Node;

internal record Road : BnbnavMessage
{
    public required string Name { get; init; }
    public required string RoadType { get; init; }
}
internal sealed record NewRoad : Road;
internal sealed record UpdatedRoad : Road;

internal sealed record NewEdge : BnbnavMessage
{
    public required string Road { get; init; }
    public required string Node1 { get; init; }
    public required string Node2 { get; init; }
}

internal sealed record NewLandmark : BnbnavMessage
{
    public required string Node { get; init; }
    public required string Name { get; init; }
    public required string LandmarkType { get; init; }
}

internal sealed record EdgeRemoved : BnbnavMessage;
internal sealed record RoadRemoved : BnbnavMessage;
internal sealed record NodeRemoved : BnbnavMessage;
internal sealed record LandmarkRemoved : BnbnavMessage;

internal sealed record UpdatedAnnotation : BnbnavMessage
{
    public required string Node { get; init; }
    public required string Name { get; init; }
    public required JsonElement Annotation { get; init; }
}

internal sealed record RemovedAnnotation : BnbnavMessage
{
    public required string Node { get; init; }
    public required string Name { get; init; }
}

internal sealed record PlayerMoved : BnbnavMessage
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
}

internal sealed record PlayerLeft : BnbnavMessage;