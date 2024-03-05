using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class NodeDeleteOperation(MapEditorService editorService, Node node) : NetworkOperation
{
    public override async Task PerformOperation()
    {
        ItemsNotToRender.Add(node);
        
        try
        {
            (await editorService.MapService!.Delete($"/nodes/{node.Id}")).AssertSuccess();
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
        var nodeBorder = (Pen)mapView.ThemeDict["NodeBorder"]!;
        var nodeBrush = (Brush)mapView.ThemeDict["NodeFill"]!;
        var rect = node.BoundingRect(mapView);
        using (context.PushOpacity(0.5))
            context.DrawRectangle(nodeBrush, nodeBorder, rect);
    }
}
