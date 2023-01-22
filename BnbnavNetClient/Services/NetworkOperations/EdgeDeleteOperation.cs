using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class EdgeDeleteOperation : NetworkOperation
{
    readonly MapEditorService _editorService;
    readonly Edge _edge;

    public EdgeDeleteOperation(MapEditorService editorService, Edge edge)
    {
        _editorService = editorService;
        _edge = edge;
    }
    
    public override async Task PerformOperation()
    {
        ItemsNotToRender.Add(_edge);
        
        try
        {
            (await _editorService.MapService!.Delete($"/edges/{_edge.Id}")).AssertSuccess();
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
        var (from, to) = _edge.Extents(mapView);
        mapView.DrawEdge(context, _edge.Road.RoadType, from, to, true);
    }
}