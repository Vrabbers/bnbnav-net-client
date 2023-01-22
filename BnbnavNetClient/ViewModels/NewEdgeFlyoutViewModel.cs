using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using BnbnavNetClient.Services.NetworkOperations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;
public sealed class NewEdgeFlyoutViewModel : ViewModel, IOpenableAsFlyout
{
    private readonly MapEditorService _mapEditorService;

    public FlyoutBase Flyout { get; set; }
    
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
    
    public NewEdgeFlyoutViewModel(MapEditorService mapEditorService, List<Node> nodesToJoin)
    {
        NodesToJoin = nodesToJoin;
        _mapEditorService = mapEditorService;
        this.WhenAnyValue(x => x.NodesToJoin, x => x.RoadPickedWithRoadSyringe).Subscribe(
            Observer.Create<System.ValueTuple<List<Node>, Road?>>(
                tuple =>
                {
                    var roads = new List<Road>();
                    if (RoadPickedWithRoadSyringe is not null) roads.Add(RoadPickedWithRoadSyringe);
                    foreach (var node in NodesToJoin)
                    {
                        // Read all edges from the map and add to list if they go to or from this node
                        roads.AddRange(mapEditorService.MapService.Edges.Values.Where(x => x.From == node || x.To == node).Select(x => x.Road));
                    }

                    FoundRoads.Clear();
                    FoundRoads.AddRange(roads.Distinct());
                }));
        this.WhenAnyValue(x => x.SelectedRoad, x => x.CurrentTabIndex).Subscribe(
            Observer.Create<System.ValueTuple<Road?, int>>(tuple =>
            {
                var canCreate = true;
                if (CurrentTabIndex == 0)
                {
                    //Waiting on implementation
                    canCreate = false;
                }
                else
                {
                    if (SelectedRoad is null) canCreate = false;
                }
                CreateButtonEnabled = canCreate;
            }));
    }

    public void ActivateRoadSyringe()
    {
        
    }

    public void CreateClicked()
    {
        for (var i = 0; i < NodesToJoin.Count - 1; i++)
        {
            var first = NodesToJoin[i];
            var second = NodesToJoin[i + 1];
            
            _mapEditorService.TrackNetworkOperation(new EdgeCreateOperation(_mapEditorService, SelectedRoad, first, second, Bidirectional));
        }
        Flyout.Hide();
    }

    public void CancelClicked()
    {
        Flyout.Hide();
    }

}
