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

namespace BnbnavNetClient.ViewModels;

public class MapViewModel : ViewModel
{
    [Reactive]
    public Point Pan { get; set; }

    [Reactive]
    public double Scale { get; set; } = 1;

    public MapService MapService { get; }

    [ObservableAsProperty]
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

    public MapViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        DeleteNodeCommand = ReactiveCommand.Create(() => { }, this.WhenAnyValue(me => me.LastRightClickHitTest).Select(list => list.Any(x => x is Node)));
        MapService = mapService;
        MapEditorService = mainViewModel.MapEditorService;
        MapEditorService.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.IsInEditMode);
    }
}

