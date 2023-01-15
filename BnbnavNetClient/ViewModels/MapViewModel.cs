using Avalonia;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;

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

    public MapViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        MapService = mapService;
        mainViewModel.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.IsInEditMode);
    }
}
