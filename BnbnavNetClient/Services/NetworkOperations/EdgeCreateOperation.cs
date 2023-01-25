using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class EdgeCreateOperation : NetworkOperation
{
    readonly MapEditorService _editorService;
    readonly Road _road;
    readonly Node _from;
    readonly Node _to;
    readonly bool _twoWay;

    public EdgeCreateOperation(MapEditorService editorService, Road road, Node from, Node to, bool twoWay)
    {
        _editorService = editorService;
        _road = road;
        _from = from;
        _to = to;
        _twoWay = twoWay;
    }
    
    public override async Task PerformOperation()
    {
        try
        {
            string roadId;
            if (_road is PendingRoad pendingRoad)
            {
                roadId = await pendingRoad.WaitForReadyTask;
            }
            else
            {
                roadId = _road.Id;
            }

            var tasks = new List<Task<MapService.ServerResponse>>
            {
                _editorService.MapService!.Submit("/edges/add", new
                {
                    Road = roadId,
                    Node1 = _from.Id,
                    Node2 = _to.Id
                })
            };

            if (_twoWay)
            {
                tasks.Add(
                    _editorService.MapService.Submit("/edges/add", new
                    {
                        Road = roadId,
                        Node1 = _to.Id,
                        Node2 = _from.Id
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
        mapView.DrawEdge(context, _road.RoadType, _from.BoundingRect(mapView).Center, _to.BoundingRect(mapView).Center, true);
    }
}