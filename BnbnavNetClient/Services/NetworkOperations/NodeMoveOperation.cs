using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using BnbnavNetClient.Models;
using BnbnavNetClient.Views;

namespace BnbnavNetClient.Services.NetworkOperations;

public class NodeMoveOperation : NetworkOperation
{
    readonly MapEditorService? _editorService;
    readonly Node _toUpdate;
    readonly Node _updateTo;

    public NodeMoveOperation(MapEditorService? editorService, Node toUpdate, Node updateTo)
    {
        _editorService = editorService;
        _toUpdate = toUpdate;
        _updateTo = updateTo;
    }

    public override async Task PerformOperation()
    {
        try
        {
            if (_editorService is not null)
            {
                (await _editorService.MapService!.Submit($"/nodes/{_toUpdate.Id}", new
                {
                    _updateTo.X,
                    _updateTo.Y,
                    _updateTo.Z
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
        var nodeBorder = (Pen)mapView.FindResource("NodeBorder")!;
        var selNodeBrush = (Brush)mapView.FindResource("SelectedNodeFill")!;

        var movingRect = _toUpdate.BoundingRect(mapView);
        var movedRect = _updateTo.BoundingRect(mapView);

        var lineBetween = new ExtendedLine(movingRect.Center, movedRect.Center);

        lineBetween = lineBetween.SetLength(20).FlipDirection().SetLength(-(lineBetween.Length - 40));

        var headLength = lineBetween.Length < 50 ? lineBetween.Length / 2 : 25;
        var arrowhead1 = lineBetween.FlipDirection().SetAngle(lineBetween.Angle - 135).SetLength(headLength);
        var arrowhead2 = lineBetween.FlipDirection().SetAngle(lineBetween.Angle + 135).SetLength(headLength);

        var geo = new PolylineGeometry();
        geo.Points.Add(arrowhead1.Point2);
        geo.Points.Add(arrowhead1.Point1);
        geo.Points.Add(arrowhead2.Point2);

        var pen = (Pen)mapView.FindResource("EditMovePen")!;
        context.DrawLine(pen, lineBetween.Point1, lineBetween.Point2);
        context.DrawGeometry(null, pen, geo);

        context.DrawRectangle(selNodeBrush, nodeBorder, _toUpdate.BoundingRect(mapView));
        context.DrawRectangle(selNodeBrush, nodeBorder, _updateTo.BoundingRect(mapView));
    }
}