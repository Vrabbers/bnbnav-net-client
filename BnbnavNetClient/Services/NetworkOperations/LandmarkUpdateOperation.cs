using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class LandmarkUpdateOperation : NetworkOperation
{
    readonly MapEditorService _editorService;
    readonly Landmark? _toUpdate;
    readonly Landmark? _updateAs;

    public LandmarkUpdateOperation(MapEditorService editorService, Landmark? toUpdate, Landmark? updateAs)
    {
        _editorService = editorService;
        _toUpdate = toUpdate;
        _updateAs = updateAs;
    }
    
    public override async Task PerformOperation()
    {

        if (_toUpdate is not null)
        {
            ItemsNotToRender.Add(_toUpdate);
            
            try
            {
                (await _editorService.MapService!.Delete($"/landmarks/{_toUpdate.Id}")).AssertSuccess();
            }
            catch (HttpRequestException)
            {
                return;
            }
            catch (NetworkOperationException)
            {
                return;
            }
        }

        if (_updateAs is not null)
        {
            try
            {
                (await _editorService.MapService!.Submit("/landmarks/add", new
                {
                    name = _updateAs.Name,
                    type = _updateAs.Type,
                    node = _updateAs.Node.Id
                })).AssertSuccess();
            }
            catch (HttpRequestException)
            {
                
            }
            catch (NetworkOperationException)
            {
                
            }
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        if (_updateAs is not null)
        {
            var rect = _updateAs.BoundingRect(mapView);
            using (context.PushOpacity(0.5))
                mapView.DrawLandmark(context, _updateAs, rect);
        }
        else if (_toUpdate is not null)
        {
            var rect = _toUpdate.BoundingRect(mapView);
            using (context.PushOpacity(0.5))
                mapView.DrawLandmark(context, _toUpdate, rect);
        }
    }
}