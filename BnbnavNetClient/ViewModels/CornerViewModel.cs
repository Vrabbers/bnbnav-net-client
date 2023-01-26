using System;
using System.Linq;
using System.Reactive;
using Avalonia.Collections;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public class CornerViewModel : ViewModel
{
    readonly MapService _mapService;

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
        _mapService = mapService;

        this.WhenAnyValue(x => x.SearchAreaFocused, x => x.SearchQuery).Subscribe(Observer.Create<ValueTuple<bool, string>>(_ =>
        {
            if (string.IsNullOrEmpty(SearchQuery) || !SearchAreaFocused)
            {
                SearchResults = new AvaloniaList<Landmark>();
                return;
            }
            
            SearchResults = new AvaloniaList<Landmark>(_mapService.Landmarks.Values.Where(x =>
                x.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)).ToList());
        }));
        this.WhenAnyValue(x => x.SearchQuery).Subscribe(Observer.Create<string>(_ =>
        {
            SelectedLandmark = null;
        }));
    }
}