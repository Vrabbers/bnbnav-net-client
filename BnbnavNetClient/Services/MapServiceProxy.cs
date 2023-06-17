namespace BnbnavNetClient.Services;

public class MapServiceProxy
{
    MapService? _mapService;

    public MapService? MapService
    {
        get => _mapService;
        set
        {
            _mapService = value;
            MapServiceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? MapServiceChanged;
}