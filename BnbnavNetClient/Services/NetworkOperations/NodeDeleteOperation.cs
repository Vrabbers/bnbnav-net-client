using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class NodeDeleteOperation : NetworkOperation
{
    readonly MapEditorService _editorService;
    readonly Node _node;

    public NodeDeleteOperation(MapEditorService editorService, Node node)
    {
        _editorService = editorService;
        _node = node;
    }
    
    public override async Task PerformOperation()
    {
        ItemsNotToRender.Add(_node);
        
        try
        {
            (await _editorService.MapService!.Delete($"/nodes/{_node.Id}")).AssertSuccess();
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
        var nodeBorder = (Pen)mapView.FindResource("NodeBorder")!;
        var nodeBrush = (Brush)mapView.FindResource("NodeFill")!;
        var rect = _node.BoundingRect(mapView);
        using (context.PushOpacity(0.5))
            context.DrawRectangle(nodeBrush, nodeBorder, rect);
    }
}
