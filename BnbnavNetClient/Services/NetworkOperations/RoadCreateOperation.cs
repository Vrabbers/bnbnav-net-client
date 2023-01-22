using System;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace BnbnavNetClient.Services.NetworkOperations;

public class RoadCreateOperation : NetworkOperation
{
    private readonly MapEditorService _editorService;
    private readonly string _name;
    private readonly RoadType _type;
    
    public PendingRoad PendingRoad { get; }

    public RoadCreateOperation(MapEditorService editorService, string name, RoadType type)
    {
        _editorService = editorService;
        _name = name;
        _type = type;

        PendingRoad = new("", name, type.ServerName());
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
            PendingRoad.ProvideId(roadResponse.Id);
        }
        catch (Exception e)
        {
            PendingRoad.SetError(e);
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        //No need to render anything for a road creation
    }

    private class RoadResponse
    {
        public string Id { get; set; }
    }
}