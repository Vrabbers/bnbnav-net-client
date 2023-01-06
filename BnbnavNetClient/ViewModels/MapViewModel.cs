using Avalonia;
using BnbnavNetClient.Services;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;
public class MapViewModel : ViewModel
{
    [Reactive]
    public Point Pan { get; set; }

    public MapService MapService { get; }

    public MapViewModel(MapService mapService)
    {
        MapService = mapService;
    }


}
