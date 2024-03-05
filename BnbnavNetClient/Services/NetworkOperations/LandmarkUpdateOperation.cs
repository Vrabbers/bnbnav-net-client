using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class LandmarkUpdateOperation(MapEditorService editorService, Landmark? toUpdate, Landmark? updateAs)
    : NetworkOperation
{
    public override async Task PerformOperation()
    {

        if (toUpdate is not null)
        {
            ItemsNotToRender.Add(toUpdate);
            
            try
            {
                (await editorService.MapService!.Delete($"/landmarks/{toUpdate.Id}")).AssertSuccess();
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

        if (updateAs is not null)
        {
            try
            {
                (await editorService.MapService!.Submit("/landmarks/add", new
                {
                    name = updateAs.Name,
                    type = updateAs.Type,
                    node = updateAs.Node.Id
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
        if (updateAs is not null)
        {
            var rect = updateAs.BoundingRect(mapView);
            using (context.PushOpacity(0.5))
                mapView.DrawLandmark(context, updateAs, rect);
        }
        else if (toUpdate is not null)
        {
            var rect = toUpdate.BoundingRect(mapView);
            using (context.PushOpacity(0.5))
                mapView.DrawLandmark(context, toUpdate, rect);
        }
    }
}