using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class NodeDeleteOperation : NetworkOperation
{
    private readonly MapEditorService _editorService;
    private readonly Node _node;

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
        catch (Exception e)
        {
            
        }
    }

    public override void Render(MapView mapView, DrawingContext context)
    {
        var nodeBorder = (Pen)mapView.FindResource("NodeBorder")!;
        var nodeBrush = (Brush)mapView.FindResource("NodeFill")!;
        using (context.PushOpacity(0.5))
            context.DrawRectangle(nodeBrush, nodeBorder, _node.BoundingRect(mapView));
    }
}