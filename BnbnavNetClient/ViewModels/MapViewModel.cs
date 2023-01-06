using Avalonia;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;
public class MapViewModel : ViewModel
{
    [Reactive]
    public Point Pan { get; set; }

    public MapService MapService { get; }
    
    [ObservableAsProperty]
    public bool IsInEditMode { get; }

    public MapViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        MapService = mapService;
        mainViewModel.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.IsInEditMode);
    }


}
