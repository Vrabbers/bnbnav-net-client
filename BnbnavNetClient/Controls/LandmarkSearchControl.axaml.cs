using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI;

namespace BnbnavNetClient.Controls;

public class LandmarkSearchControl : TemplatedControl
{
    Landmark? _SelectedLandmark;
    public static readonly DirectProperty<LandmarkSearchControl, Landmark?> SelectedLandmarkProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, Landmark?>("SelectedLandmark", o => o.SelectedLandmark, (o, v) => o.SelectedLandmark = v);
    // public static readonly DirectProperty<LandmarkSearchControl, object?> SelectedLandmarkProperty =
    //     ListBox.SelectedItemProperty.AddOwner<LandmarkSearchControl>(x => x.SelectedLandmark,
    //         (x, v) => x.SelectedLandmark = v);
    
    MapService _MapService = null!;
    public static readonly DirectProperty<LandmarkSearchControl, MapService> MapServiceProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, MapService>("MapService", o => o.MapService, (o, v) => o.MapService = v);

    private string _searchQuery = null!;
    public static readonly DirectProperty<LandmarkSearchControl, string> SearchQueryProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, string>(
        "SearchQuery", o => o.SearchQuery, (o, v) => o.SearchQuery = v);

    private AvaloniaList<Landmark> _searchResults = new();

    public static readonly DirectProperty<LandmarkSearchControl, AvaloniaList<Landmark>> SearchResultsProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, AvaloniaList<Landmark>>(
        "SearchResults", o => o.SearchResults, (o, v) => o.SearchResults = v);

    public AvaloniaList<Landmark> SearchResults
    {
        get => _searchResults;
        set => SetAndRaise(SearchResultsProperty, ref _searchResults, value);
    }
    
    public string SearchQuery
    {
        get => _searchQuery;
        set => SetAndRaise(SearchQueryProperty, ref _searchQuery, value);
    }

    public Landmark? SelectedLandmark
    {
        get { return _SelectedLandmark; }
        set { SetAndRaise(SelectedLandmarkProperty, ref _SelectedLandmark, value); }
    }

    public MapService MapService
    {
        get { return _MapService; }
        set { SetAndRaise(MapServiceProperty, ref _MapService, value); }
    }
}