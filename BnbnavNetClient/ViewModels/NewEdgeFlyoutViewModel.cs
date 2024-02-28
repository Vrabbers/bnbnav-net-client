using System.Reactive;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using BnbnavNetClient.Services.NetworkOperations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public class RoadTypeHelper
{
    public RoadType RoadType { get; }

    public RoadTypeHelper(RoadType roadType)
    {
        RoadType = roadType;
    }

    public string HumanReadableName => RoadType.HumanReadableName();
}

public sealed class NewEdgeFlyoutViewModel : ViewModel, IOpenableAsFlyout
{
    readonly MapEditorService _mapEditorService;

    [Reactive]
    public FlyoutBase? Flyout { get; set; }
    
    [Reactive]
    public bool Bidirectional { get; set; } = true;

    [Reactive]
    public List<Node> NodesToJoin { get; set; }
    
    [Reactive]
    public Road? RoadPickedWithRoadSyringe { get; set; }

    [Reactive]
    public AvaloniaList<Road> FoundRoads { get; set; } = new();
    
    [Reactive]
    public Road? SelectedRoad { get; set; }
    
    [Reactive]
    public bool CreateButtonEnabled { get; set; }

    [Reactive]
    public int CurrentTabIndex { get; set; }
    
    [Reactive]
    public AvaloniaList<RoadTypeHelper> RoadTypes { get; set; } = new();

    [Reactive]
    public RoadTypeHelper? SelectedRoadType { get; set; }

    [Reactive]
    public string NewRoadName { get; set; } = string.Empty;

    public NewEdgeFlyoutViewModel(MapEditorService mapEditorService, List<Node> nodesToJoin)
    {
        NodesToJoin = nodesToJoin;
        _mapEditorService = mapEditorService;

        RoadTypes.AddRange(Enum.GetValues<RoadType>().Skip(1).Select(x => new RoadTypeHelper(x)));

        this.WhenAnyValue(x => x.NodesToJoin, x => x.RoadPickedWithRoadSyringe).Subscribe(
            Observer.Create<ValueTuple<List<Node>, Road?>>(
                _ =>
                {
                    var roads = new List<Road>();
                    if (RoadPickedWithRoadSyringe is not null) roads.Add(RoadPickedWithRoadSyringe);
                    foreach (var node in NodesToJoin)
                    {
                        // Read all edges from the map and add to list if they go to or from this node
                        roads.AddRange(mapEditorService.MapService!.Edges.Values.Where(x => x.From == node || x.To == node).Select(x => x.Road));
                    }

                    FoundRoads.Clear();
                    FoundRoads.AddRange(roads.Distinct());
                }));
        this.WhenAnyValue(x => x.SelectedRoad, x => x.CurrentTabIndex, x => x.SelectedRoadType, x => x.NewRoadName).Subscribe(
            Observer.Create<ValueTuple<Road?, int, RoadTypeHelper?, string>>(_ =>
            {
                var canCreate = true;
                if (CurrentTabIndex == 0)
                {
                    //Waiting on implementation
                    if (SelectedRoadType is null || string.IsNullOrWhiteSpace(NewRoadName)) canCreate = false;
                }
                else
                {
                    if (SelectedRoad is null) canCreate = false;
                }
                CreateButtonEnabled = canCreate;
            }));
    }

    public void CreateClicked()
    {
        Road road;
        if (CurrentTabIndex == 0) //New Road
        {
            var roadOp = new RoadCreateOperation(_mapEditorService, NewRoadName, SelectedRoadType!.RoadType);
            _mapEditorService.TrackNetworkOperation(roadOp);

            road = roadOp.PendingRoad;
        }
        else //Existing Road
        {
            road = SelectedRoad!;
        }
        
        for (var i = 0; i < NodesToJoin.Count - 1; i++)
        {
            var first = NodesToJoin[i];
            var second = NodesToJoin[i + 1];
            
            _mapEditorService.TrackNetworkOperation(new EdgeCreateOperation(_mapEditorService, road, first, second, Bidirectional));
        }
        Flyout?.Hide();
    }

    public void CancelClicked()
    {
        Flyout?.Hide();
    }

}
