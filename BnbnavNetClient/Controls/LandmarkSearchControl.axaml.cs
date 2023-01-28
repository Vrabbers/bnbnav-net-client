using System;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI;

namespace BnbnavNetClient.Controls;

public class LandmarkSearchControl : TemplatedControl
{
    Landmark? _SelectedLandmark;
    public static readonly DirectProperty<LandmarkSearchControl, Landmark?> SelectedLandmarkProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, Landmark?>("SelectedLandmark", o => o.SelectedLandmark, (o, v) => o.SelectedLandmark = v, defaultBindingMode: BindingMode.TwoWay);
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

    public LandmarkSearchControl()
    {
        SearchQueryProperty.Changed.Subscribe(x =>
        {
            SelectedLandmark = null;
            
            if (string.IsNullOrEmpty(SearchQuery))
            {
                SearchResults = new AvaloniaList<Landmark>();
                return;
            }

            var listItems = MapService.Landmarks.Values.Where(x =>
                x.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));

            var coordinate = TemporaryLandmark.ParseCoordinateString(SearchQuery);
            if (coordinate is not null) listItems = listItems.Prepend(coordinate);
            
            SearchResults = new AvaloniaList<Landmark>(listItems);
        });

        SelectedLandmarkProperty.Changed.Subscribe(_ =>
        {

        });
    }
}