using Avalonia.Collections;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public class LandmarkSearchControlViewModel : ViewModel
{
    
    [Reactive]
    public string SearchQuery { get; set; } = string.Empty;

    [Reactive]
    public AvaloniaList<Landmark> SearchResults { get; set; } = new();

    [Reactive]
    public bool SearchAreaFocused { get; set; } = true;
    
    [Reactive]
    public Landmark? SelectedLandmark { get; set; }

    public MapService? MapService { get; set; }

    public void ClearSearchQuery()
    {
        SearchQuery = string.Empty;
    }
}