using Avalonia;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;
public class MapViewModel : ViewModel
{
    [Reactive]
    public Point Pan { get; set; }

    [Reactive]
    public double Scale { get; set; } = 1;

    [ObservableAsProperty]
    public MapService MapService { get; }
    
    [ObservableAsProperty]
    public bool IsInEditMode { get; }

    [Reactive]
    // in radians!
    public double Rotation { get; set; } = 0;

    public MapViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        MapService = mapService;
        mainViewModel.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.IsInEditMode);
    }


}
