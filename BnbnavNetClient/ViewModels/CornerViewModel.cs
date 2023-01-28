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
    public ISearchable? SelectedLandmark { get; set; }
    
    [Reactive]
    public ISearchable? GoModeStartPoint { get; set; }
    
    [Reactive]
    public ISearchable? GoModeEndPoint { get; set; }

    public CornerViewModel(MapService mapService)
    {
        MapService = mapService;

        this.WhenAnyValue(x => x.CurrentUi).Subscribe(Observer.Create<AvailableUi>(_ =>
        {
            IsInSearchMode = CurrentUi == AvailableUi.Search;
            IsInPrepareMode = CurrentUi == AvailableUi.Prepare;
            IsInGoMode = CurrentUi == AvailableUi.Go;
        }));
    }

    public void GetDirectionsToSelectedLandmark()
    {
        GoModeEndPoint = SelectedLandmark;
        CurrentUi = AvailableUi.Prepare;
    }

    public void LeavePrepareMode()
    {
        CurrentUi = AvailableUi.Search;
    }

}