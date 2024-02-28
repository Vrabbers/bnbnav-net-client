using Avalonia;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using BnbnavNetClient.Services.NetworkOperations;
using JetBrains.Annotations;

namespace BnbnavNetClient.ViewModels;

public class MapViewModel : ViewModel
{
    readonly MainViewModel _mainViewModel;

    [Reactive]
    public Point Pan { get; set; }

    [Reactive]
    public double Scale { get; set; } = 1;

    public MapService MapService { get; }

    [ObservableAsProperty] 
    [UsedImplicitly] 
    public bool IsInEditMode { get; }

    // In degrees!
    [Reactive]
    public double Rotation { get; set; } = 0;

    // As a % of the window bounds
    [Reactive]
    public Vector RotationOrigin { get; set; } = new(0.5, 0.5);
    
    [Reactive]
    public string Test { get; set; } = string.Empty;

    public MapEditorService MapEditorService { get; set; }

    [Reactive]
    public ViewModel? FlyoutViewModel { get; set; }

    [Reactive]
    public IReadOnlyList<MapItem> LastRightClickHitTest { get; set; } = Array.Empty<MapItem>();

    public ReactiveCommand<Unit, Unit> DeleteNodeCommand { get; }
    
    [ObservableAsProperty]
    public bool FollowMeEnabled { get; set; }

    [ObservableAsProperty] 
    public string? LoggedInUsername { get; set; }

    [Reactive] 
    public AvaloniaList<MenuItem> ContextMenuItems { get; set; } = [];
    
    [Reactive]
    public ISearchable? SelectedLandmark { get; set; }
    
    [Reactive]
    public ISearchable? GoModeStartPoint { get; set; }
    
    [Reactive]
    public ISearchable? GoModeEndPoint { get; set; }
    
    [Reactive]
    public bool HighlightInterWorldNodesEnabled { get; set; }

    [Reactive]
    public AvailableUi CurrentUi { get; set; } = AvailableUi.Search;

    [ObservableAsProperty] 
    public string ChosenWorld { get; set; }

    // Initialising ChosenWorld causes a compile time error
#pragma warning disable CS8618
    public MapViewModel(MapService mapService, MainViewModel mainViewModel)
#pragma warning restore CS8618
    {
        _mainViewModel = mainViewModel;
        DeleteNodeCommand = ReactiveCommand.Create(() => { }, this.WhenAnyValue(me => me.LastRightClickHitTest).Select(list => list.Any(x => x is Node)));
        MapService = mapService;
        MapEditorService = mainViewModel.MapEditorService;
        MapEditorService.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.IsInEditMode);

        mainViewModel.WhenAnyValue(x => x.FollowMeEnabled).ToPropertyEx(this, x => x.FollowMeEnabled);
        mainViewModel.WhenAnyValue(x => x.LoggedInUsername).ToPropertyEx(this, x => x.LoggedInUsername);
        mainViewModel.WhenAnyValue(x => x.ChosenWorld).ToPropertyEx(this, x => x.ChosenWorld);
    }
    
    public void QueueDelete(params MapItem[] mapItems)
    {
        foreach (var mapItem in mapItems)
        {
            switch (mapItem)
            {
                case Node node:
                    MapEditorService.TrackNetworkOperation(new NodeDeleteOperation(MapEditorService, node));
                    break;
                case Edge edge:
                {
                    MapEditorService.TrackNetworkOperation(new EdgeDeleteOperation(MapEditorService, edge));
                    if (MapService.OppositeEdge(edge) is { } opposite)
                    {
                        MapEditorService.TrackNetworkOperation(new EdgeDeleteOperation(MapEditorService, opposite));
                    }

                    break;
                }
            }
        }
    }

    public void DisableFollowMe()
    {
        _mainViewModel.FollowMeEnabled = false;
    }

    public void ChangeWorld(string world)
    {
        _mainViewModel.ChosenWorld = world;
    }
}

