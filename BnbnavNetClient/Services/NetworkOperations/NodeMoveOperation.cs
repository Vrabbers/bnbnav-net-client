using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class NodeMoveOperation(MapEditorService? editorService, Node toUpdate, Node updateTo)
    : NetworkOperation
{
    public override async Task PerformOperation()
    {
        try
        {
            if (editorService is not null)
            {
                (await editorService.MapService!.Submit($"/nodes/{toUpdate.Id}", new
                {
                    updateTo.X,
                    updateTo.Y,
                    updateTo.Z
                })).AssertSuccess();
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
        var nodeBorder = (Pen)mapView.ThemeDict["NodeBorder"]!;
        var selNodeBrush = (Brush)mapView.ThemeDict["SelectedNodeFill"]!;

        var movingRect = toUpdate.BoundingRect(mapView);
        var movedRect = updateTo.BoundingRect(mapView);

        var lineBetween = new ExtendedLine(movingRect.Center, movedRect.Center);

        lineBetween = lineBetween.SetLength(20).FlipDirection().SetLength(-(lineBetween.Length - 40));

        var headLength = lineBetween.Length < 50 ? lineBetween.Length / 2 : 25;
        var arrowhead1 = lineBetween.FlipDirection().SetAngle(lineBetween.Angle - 135).SetLength(headLength);
        var arrowhead2 = lineBetween.FlipDirection().SetAngle(lineBetween.Angle + 135).SetLength(headLength);

        var geo = new PolylineGeometry();
        geo.Points.Add(arrowhead1.Point2);
        geo.Points.Add(arrowhead1.Point1);
        geo.Points.Add(arrowhead2.Point2);

        var pen = (Pen)mapView.ThemeDict["EditMovePen"]!;
        context.DrawLine(pen, lineBetween.Point1, lineBetween.Point2);
        context.DrawGeometry(null, pen, geo);

        context.DrawRectangle(selNodeBrush, nodeBorder, toUpdate.BoundingRect(mapView));
        context.DrawRectangle(selNodeBrush, nodeBorder, updateTo.BoundingRect(mapView));
    }
}