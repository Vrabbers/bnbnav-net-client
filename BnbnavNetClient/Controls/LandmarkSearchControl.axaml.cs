using System;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;

namespace BnbnavNetClient.Controls;

public class LandmarkSearchControl : TemplatedControl
{
    ISearchable? _selectedLandmark;
    public static readonly DirectProperty<LandmarkSearchControl, ISearchable?> SelectedLandmarkProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, ISearchable?>("SelectedLandmark", o => o.SelectedLandmark, (o, v) => o.SelectedLandmark = v, defaultBindingMode: BindingMode.TwoWay);
    MapService _mapService = null!;
    public static readonly DirectProperty<LandmarkSearchControl, MapService> MapServiceProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, MapService>("MapService", o => o.MapService, (o, v) => o.MapService = v);

    string _searchQuery = null!;
    public static readonly DirectProperty<LandmarkSearchControl, string> SearchQueryProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, string>(
        "SearchQuery", o => o.SearchQuery, (o, v) => o.SearchQuery = v);

    AvaloniaList<ISearchable> _searchResults = new();

    public static readonly DirectProperty<LandmarkSearchControl, AvaloniaList<ISearchable>> SearchResultsProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchControl, AvaloniaList<ISearchable>>(
        "SearchResults", o => o.SearchResults, (o, v) => o.SearchResults = v);

    public AvaloniaList<ISearchable> SearchResults
    {
        get => _searchResults;
        set => SetAndRaise(SearchResultsProperty, ref _searchResults, value);
    }
    
    public string SearchQuery
    {
        get => _searchQuery;
        set => SetAndRaise(SearchQueryProperty, ref _searchQuery, value);
    }

    public ISearchable? SelectedLandmark
    {
        get { return _selectedLandmark; }
        set { SetAndRaise(SelectedLandmarkProperty, ref _selectedLandmark, value); }
    }

    public MapService MapService
    {
        get { return _mapService; }
        set { SetAndRaise(MapServiceProperty, ref _mapService, value); }
    }

    public LandmarkSearchControl()
    {
        SearchQueryProperty.Changed.Subscribe(_ =>
        {
            if (SelectedLandmark?.Name == SearchQuery)
            {
                return;
            }

            SelectedLandmark = null;

            if (string.IsNullOrEmpty(SearchQuery))
            {
                SearchResults = new AvaloniaList<ISearchable>();
                return;
            }

            var listItems = MapService.Players.Values.Where(x =>
                    x.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)).Cast<ISearchable>()
                .Union(MapService.Landmarks.Values.Where(x =>
                    x.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)));

            var coordinate = TemporaryLandmark.ParseCoordinateString(SearchQuery);
            if (coordinate is not null) listItems = listItems.Prepend(coordinate);
            
            SearchResults = new AvaloniaList<ISearchable>(listItems);
        });

        SelectedLandmarkProperty.Changed.Subscribe(_ =>
        {
            if (SelectedLandmark is not null)
            {
                SearchQuery = SelectedLandmark.Name;
            }
        });
    }
}