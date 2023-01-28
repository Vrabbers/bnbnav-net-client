using Avalonia;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using BnbnavNetClient.Services.NetworkOperations;
using JetBrains.Annotations;

namespace BnbnavNetClient.ViewModels;

public class MapViewModel : ViewModel
{
    private readonly MainViewModel _mainViewModel;

    [Reactive]
    public Point Pan { get; set; }

    [Reactive]
    public double Scale { get; set; } = 1;

    public MapService MapService { get; }

    [ObservableAsProperty] 
    [UsedImplicitly] 
    public bool IsInEditMode { get; }

    // In radians!
    [Reactive]
    public double Rotation { get; set; } = 0;

    [Reactive]
    public bool NightMode { get; set; } = false;

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

    [Reactive] public AvaloniaList<MenuItem> ContextMenuItems { get; set; } = new();
    
    [ObservableAsProperty]
    public ISearchable? SelectedLandmark { get; set; }

    public MapViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        DeleteNodeCommand = ReactiveCommand.Create(() => { }, this.WhenAnyValue(me => me.LastRightClickHitTest).Select(list => list.Any(x => x is Node)));
        MapService = mapService;
        MapEditorService = mainViewModel.MapEditorService;
        MapEditorService.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.IsInEditMode);

        mainViewModel.WhenAnyValue(x => x.FollowMeEnabled).ToPropertyEx(this, x => x.FollowMeEnabled);
        mainViewModel.WhenAnyValue(x => x.LoggedInUsername).ToPropertyEx(this, x => x.LoggedInUsername);
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
}

