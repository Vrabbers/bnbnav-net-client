using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class EdgeCreateOperation(MapEditorService editorService, Road road, Node from, Node to, bool twoWay)
    : NetworkOperation
{
    public override async Task PerformOperation()
    {
        try
        {
            string roadId;
            if (road is PendingRoad pendingRoad)
            {
                roadId = await pendingRoad.WaitForReadyTask;
            }
            else
            {
                roadId = road.Id;
            }

            var tasks = new List<Task<MapService.ServerResponse>>
            {
                editorService.MapService!.Submit("/edges/add", new
                {
                    Road = roadId,
                    Node1 = from.Id,
                    Node2 = to.Id
                })
            };

            if (twoWay)
            {
                tasks.Add(
                    editorService.MapService.Submit("/edges/add", new
                    {
                        Road = roadId,
                        Node1 = to.Id,
                        Node2 = from.Id
                    }));
            }

            var resolved = await Task.WhenAll(tasks);
            foreach (var response in resolved)
            {
                response.AssertSuccess();
            }
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
        mapView.DrawEdge(context, road.RoadType, from.BoundingRect(mapView).Center, to.BoundingRect(mapView).Center, true);
    }
}