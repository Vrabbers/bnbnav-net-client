using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class RoadUpdateOperation : NetworkOperation
{
    private readonly MapEditorService _editorService;
    private readonly Road _road;
    private readonly string _newName;
    private readonly RoadType _newRoadType;

    public RoadUpdateOperation(MapEditorService editorService, Road road, string newName, RoadType newRoadType)
    {
        _editorService = editorService;
        _road = road;
        _newName = newName;
        _newRoadType = newRoadType;
    }
    
    public override async Task PerformOperation()
    {
        try
        {
            (await _editorService.MapService!.Submit($"/roads/{_road.Id}", new
            {
                name = _newName,
                type = _newRoadType.ServerName()
            })).AssertSuccess();
        }
        catch (HttpRequestException)
        {
            
        }
        catch (NetworkOperationException)
        {
            
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        //noop
    }
}