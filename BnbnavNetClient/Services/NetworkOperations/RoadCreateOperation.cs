using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BnbnavNetClient.Services.NetworkOperations;

public class RoadCreateOperation : NetworkOperation
{
    readonly MapEditorService _editorService;
    readonly string _name;
    readonly RoadType _type;
    
    public PendingRoad PendingRoad { get; }

    public RoadCreateOperation(MapEditorService editorService, string name, RoadType type)
    {
        _editorService = editorService;
        _name = name;
        _type = type;

        PendingRoad = new PendingRoad("", name, type.ServerName());
    }
    
    public override async Task PerformOperation()
    {
        try
        {
            var response = (await _editorService.MapService!.Submit($"/roads/add", new
            {
                Name = _name,
                Type = _type.ServerName()
            })).AssertSuccess();

            var roadResponse = await JsonSerializer.DeserializeAsync<RoadResponse>(response.Stream, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            });
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

    class RoadResponse
    {
        public required string Id { get; set; }
    }
}