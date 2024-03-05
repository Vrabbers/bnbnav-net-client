using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class EdgeDeleteOperation(MapEditorService editorService, Edge edge) : NetworkOperation
{
    public override async Task PerformOperation()
    {
        ItemsNotToRender.Add(edge);
        
        try
        {
            (await editorService.MapService!.Delete($"/edges/{edge.Id}")).AssertSuccess();
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
        var (from, to) = edge.Extents(mapView);
        mapView.DrawEdge(context, edge.Road.RoadType, from, to, true);
    }
}