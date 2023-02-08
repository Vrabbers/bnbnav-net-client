using System;
using System.Linq;
using System.Reactive;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using BnbnavNetClient.Services.NetworkOperations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public class RoadEditViewModel : ViewModel, IOpenableAsFlyout
{
    readonly MapEditorService _editorService;
    readonly Road _road;

    [Reactive]
    public AvaloniaList<RoadTypeHelper> RoadTypes { get; set; } = new();

    [Reactive]
    public RoadTypeHelper? SelectedRoadType { get; set; }
    
    [Reactive]
    public string RoadName { get; set; }
    
    [Reactive]
    public bool UpdateButtonEnabled { get; set; }

    public RoadEditViewModel(MapEditorService editorService, Road road)
    {
        _editorService = editorService;
        _road = road;
        
        RoadTypes.AddRange(Enum.GetValues<RoadType>().Skip(1).Select(x => new RoadTypeHelper(x)));

        RoadName = road.Name;
        SelectedRoadType = RoadTypes.SingleOrDefault(x => x.RoadType == road.RoadType);

        this.WhenAnyValue(x => x.SelectedRoadType, x => x.RoadName).Subscribe(
            Observer.Create<ValueTuple<RoadTypeHelper?, string>>(_ =>
                UpdateButtonEnabled = SelectedRoadType is not null && !string.IsNullOrWhiteSpace(RoadName)));
    }

    public FlyoutBase? Flyout { get; set; }

    public void CancelClicked()
    {
        Flyout!.Hide();
    }

    public void UpdateClicked()
    {
        _editorService.TrackNetworkOperation(new RoadUpdateOperation(_editorService, _road, RoadName, SelectedRoadType!.RoadType));
        Flyout!.Hide();
    }
}