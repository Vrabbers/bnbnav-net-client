using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BnbnavNetClient.Services;

sealed class BnbnavWebsocketService
{
    ClientWebSocket _ws = null!;

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
                    },
                    Path = "ws"
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

    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async IAsyncEnumerable<BnbnavMessage> GetMessages([EnumeratorCancellation] CancellationToken token)
    {
        ReadOnlyMemory<byte> buf;
        while ((buf = await NextMessageAsync(token)).Length != 0)
        {
            var message = JsonSerializer.Deserialize<BnbnavMessage>(buf.Span, JsonOptions);
            if (message is not null && message.GetType() != typeof(BnbnavMessage))
            {
                yield return message;
            }
        }
    }

    async Task<ReadOnlyMemory<byte>> NextMessageAsync(CancellationToken ct)
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
[JsonDerivedType(typeof(NodeCreated), "newNode")]
[JsonDerivedType(typeof(RoadCreated), "newRoad")]
[JsonDerivedType(typeof(EdgeCreated), "newEdge")]
[JsonDerivedType(typeof(LandmarkCreated), "newLandmark")]
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
public record BnbnavMessage
{
    public string? Id { get; init; }
}

abstract record NodeMessage : BnbnavMessage
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Z { get; init; }

    public required string Player { get; init; }
}

sealed record NodeCreated : NodeMessage;
sealed record UpdatedNode : NodeMessage;

abstract record RoadMessage : BnbnavMessage
{
    public required string Name { get; init; }
    public required string RoadType { get; init; }
}
sealed record RoadCreated : RoadMessage;
sealed record UpdatedRoad : RoadMessage;

sealed record EdgeCreated : BnbnavMessage
{
    public required string Road { get; init; }
    public required string Node1 { get; init; }
    public required string Node2 { get; init; }
}

sealed record LandmarkCreated : BnbnavMessage
{
    public required string Node { get; init; }
    public required string Name { get; init; }
    public required string LandmarkType { get; init; }
}

sealed record EdgeRemoved : BnbnavMessage;
sealed record RoadRemoved : BnbnavMessage;
sealed record NodeRemoved : BnbnavMessage;
sealed record LandmarkRemoved : BnbnavMessage;

sealed record UpdatedAnnotation : BnbnavMessage
{
    public required string Node { get; init; }
    public required string Name { get; init; }
    public required JsonElement Annotation { get; init; }
}

sealed record RemovedAnnotation : BnbnavMessage
{
    public required string Node { get; init; }
    public required string Name { get; init; }
}

public sealed record PlayerMoved : BnbnavMessage
{
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
}

sealed record PlayerLeft : BnbnavMessage;