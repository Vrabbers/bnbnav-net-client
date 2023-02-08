using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class RoadUpdateOperation : NetworkOperation
{
    readonly MapEditorService _editorService;
    readonly Road _road;
    readonly string _newName;
    readonly RoadType _newRoadType;
    readonly List<Edge> _affectedEdges = new();

    public RoadUpdateOperation(MapEditorService editorService, Road road, string newName, RoadType newRoadType)
    {
        _editorService = editorService;
        _road = road;
        _newName = newName;
        _newRoadType = newRoadType;
        
        _affectedEdges.AddRange(editorService.MapService!.Edges.Values.Where(x => x.Road == road));
    }
    
    public override async Task PerformOperation()
    {
        ItemsNotToRender.AddRange(_affectedEdges);
        
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
        foreach (var edge in _affectedEdges)
        {
            mapView.DrawEdge(context, _newRoadType, edge.From.BoundingRect(mapView).Center, edge.To.BoundingRect(mapView).Center, true);
        }
    }
}