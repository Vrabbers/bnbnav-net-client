using System;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using Avalonia.Collections;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public class CornerViewModel : ViewModel
{
    public MapService MapService { get; }

    public enum AvailableUi
    {
        Search,
        Prepare,
        Go
    }
    
    [Reactive]
    public AvailableUi CurrentUi { get; set; } = AvailableUi.Search;
    
    [Reactive]
    public bool IsInSearchMode { get; set; }
    
    [Reactive]
    public bool IsInPrepareMode { get; set; }
    
    [Reactive]
    public bool IsInGoMode { get; set; }
    
    [Reactive]
    public string SearchQuery { get; set; } = string.Empty;

    [Reactive]
    public AvaloniaList<Landmark> SearchResults { get; set; } = new();

    [Reactive]
    public bool SearchAreaFocused { get; set; } = true;
    
    [Reactive]
    public Landmark? SelectedLandmark { get; set; }

    public CornerViewModel(MapService mapService)
    {
        MapService = mapService;

        this.WhenAnyValue(x => x.SearchAreaFocused, x => x.SearchQuery).Subscribe(Observer.Create<ValueTuple<bool, string>>(_ =>
        {
            if (string.IsNullOrEmpty(SearchQuery) || !SearchAreaFocused)
            {
                SearchResults = new AvaloniaList<Landmark>();
                return;
            }

            var listItems = MapService.Landmarks.Values.Where(x =>
                x.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));

            var coordinate = TemporaryLandmark.ParseCoordinateString(SearchQuery);
            if (coordinate is not null) listItems = listItems.Prepend(coordinate);
            
            SearchResults = new AvaloniaList<Landmark>(listItems);
        }));
        this.WhenAnyValue(x => x.SearchQuery).Subscribe(Observer.Create<string>(_ =>
        {
            SelectedLandmark = null;
        }));
        this.WhenAnyValue(x => x.CurrentUi).Subscribe(Observer.Create<AvailableUi>(_ =>
        {
            IsInSearchMode = CurrentUi == AvailableUi.Search;
            IsInPrepareMode = CurrentUi == AvailableUi.Prepare;
            IsInGoMode = CurrentUi == AvailableUi.Go;
        }));
    }

    public void GetDirectionsToSelectedLandmark()
    {
        CurrentUi = AvailableUi.Prepare;
    }

    public void LeavePrepareMode()
    {
        CurrentUi = AvailableUi.Search;
    }

}