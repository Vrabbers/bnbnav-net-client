using System.Text.Json;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BnbnavNetClient.Services.NetworkOperations;

public class RoadCreateOperation(MapEditorService editorService, string name, RoadType type) : NetworkOperation
{
    public PendingRoad PendingRoad { get; } = new("", name, type.ServerName());


    static readonly JsonSerializerOptions RoadResponseSerializerOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };
    public override async Task PerformOperation()
    {
        try
        {
            var response = (await editorService.MapService!.Submit($"/roads/add", new
            {
                Name = name,
                Type = type.ServerName()
            })).AssertSuccess();

            var roadResponse = await JsonSerializer.DeserializeAsync<RoadResponse>(response.Stream, RoadResponseSerializerOptions);
            PendingRoad.ProvideId(roadResponse!.Id);
        }
        catch (HttpRequestException e)
        {
            PendingRoad.SetError(e);
        }
        catch (NetworkOperationException e)
        {
            PendingRoad.SetError(e);
        }
        catch (Exception e)
        {
            PendingRoad.SetError(e);
            throw;
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        //No need to render anything for a road creation
    }

    sealed class RoadResponse
    {
        public required string Id { get; set; }
    }
}