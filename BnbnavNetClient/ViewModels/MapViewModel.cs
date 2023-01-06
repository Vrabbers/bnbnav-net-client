using Avalonia;
using BnbnavNetClient.Services;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;
public class MapViewModel : ViewModel
{
    [Reactive]
    public Point Pan { get; set; }

    [Reactive]
    public double Scale { get; set; } = 1;
    
    public MapService MapService { get; }

    public MapViewModel(MapService mapService)
    {
        MapService = mapService;
    }


}
