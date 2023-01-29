using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using Avalonia.Collections;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public enum AvailableUi
{
    Search,
    Prepare,
    Go
}

public class CornerViewModel : ViewModel
{
    public MapService MapService { get; }

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
    
    [ObservableAsProperty]
    public string? LoggedInUsername { get; set; }

    public CornerViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        MapService = mapService;

        this.WhenAnyValue(x => x.CurrentUi).Subscribe(Observer.Create<AvailableUi>(_ =>
        {
            IsInSearchMode = CurrentUi == AvailableUi.Search;
            IsInPrepareMode = CurrentUi == AvailableUi.Prepare;
            IsInGoMode = CurrentUi == AvailableUi.Go;
        }));
        this.WhenAnyValue(x => x.GoModeEndPoint).Subscribe(Observer.Create<ISearchable?>(_ =>
        {
            if (GoModeStartPoint is not null) 
                return;
            if (string.IsNullOrEmpty(LoggedInUsername))
                return;

            //Attempt to find the player
            var playerExists = MapService.Players.TryGetValue(LoggedInUsername, out var player);
            if (playerExists)
            {
                GoModeStartPoint = player;
            }
        }));
        this.WhenAnyValue(x => x.GoModeStartPoint, x => x.GoModeEndPoint).Subscribe(Observer.Create<
            // ReSharper disable once AsyncVoidLambda
            ValueTuple<ISearchable?, ISearchable?>>(async _ =>
                {
                    if (GoModeStartPoint is null || GoModeEndPoint is null)
                    {
                        //Clear the current route
                        MapService.CurrentRoute = null;
                        return;
                    }

                    try
                    {
                        var route = await MapService.ObtainCalculatedRoute(GoModeStartPoint, GoModeEndPoint);
                        MapService.CurrentRoute = route;

                        foreach (var inst in route.Instructions)
                        {
                            Console.WriteLine(inst.HumanReadableString((int) inst.distance));
                        }
                    }
                    catch (RoutingException ex)
                    {
                        
                    }
                }));
        
        mainViewModel.WhenAnyValue(x => x.LoggedInUsername).ToPropertyEx(this, x => x.LoggedInUsername);
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