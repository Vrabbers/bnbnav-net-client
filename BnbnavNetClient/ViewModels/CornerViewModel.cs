using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia;
using Avalonia.Collections;
using BnbnavNetClient.I18Next.Services;
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
    readonly MainViewModel _mainViewModel;
    readonly IAvaloniaI18Next _i18n;
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

    CancellationTokenSource? RouteCalculationCancellationSource { get; set; }

    [Reactive]
    public bool CalculatingRoute { get; set; }

    [Reactive]
    public string? RouteCalculationError { get; set; }

    [ObservableAsProperty] 
    public int BlocksToNextInstruction { get; }

    [ObservableAsProperty]
    public CalculatedRoute.Instruction? CurrentInstruction { get; }

    [Reactive]
    public string BlocksToRouteEnd { get; set; }

    public CornerViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        MapService = mapService;
        _i18n = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();

        this.WhenAnyValue(x => x.CurrentUi).Subscribe(Observer.Create<AvailableUi>(_ =>
        {
            IsInSearchMode = CurrentUi == AvailableUi.Search;
            IsInPrepareMode = CurrentUi == AvailableUi.Prepare;
            IsInGoMode = CurrentUi == AvailableUi.Go;
            SetupRouteForGoMode();
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
                    RouteCalculationCancellationSource?.Cancel();
                    RouteCalculationCancellationSource = new CancellationTokenSource();
                    
                    //Clear the current route
                    MapService.CurrentRoute = null;
                    RouteCalculationError = null;
                    
                    if (GoModeStartPoint is null || GoModeEndPoint is null)
                    {
                        return;
                    }

                    try
                    {
                        CalculatingRoute = true;
                        var route = await MapService.ObtainCalculatedRoute(GoModeStartPoint, GoModeEndPoint,
                            RouteCalculationCancellationSource.Token);
                        MapService.CurrentRoute = route;
                        CalculatingRoute = false;
                        
                        RouteCalculationCancellationSource = null;

                        foreach (var inst in route.Instructions)
                        {
                            Console.WriteLine(inst.HumanReadableString((int)inst.distance));
                        }
                    }
                    catch (NoSuitableEdgeException ex)
                    {
                        CalculatingRoute = false;
                        RouteCalculationError = _i18n["DIRECTIONS_CALCULATING_FAILURE_NO_ROAD"];
                    }
                    catch (DisjointNetworkException ex)
                    {
                        CalculatingRoute = false;
                        RouteCalculationError = _i18n["DIRECTIONS_CALCULATING_FAILURE_NO_PATH"];
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore
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

    public void EnterGoMode()
    {
        _mainViewModel.Popup = new AlertDialogViewModel()
        {
            Title = _i18n["GO_MODE_WARNING_TITLE"],
            Message = _i18n["GO_MODE_WARNING_MESSAGE"],
            Ok = ReactiveCommand.Create(() =>
            {
                _mainViewModel.Popup = null;

                CurrentUi = AvailableUi.Go;
            })
        };
    }

    public void LeaveGoMode()
    {
        CurrentUi = AvailableUi.Prepare;
    }

    void SetupRouteForGoMode()
    {
        if (MapService.CurrentRoute is null)
        {
            return;
        }

        if (IsInGoMode)
        {
            MapService.CurrentRoute.WhenAnyValue(x => x.CurrentInstruction)
                .ToPropertyEx(this, x => x.CurrentInstruction);
            MapService.CurrentRoute.WhenAnyValue(x => x.BlocksToNextInstruction)
                .ToPropertyEx(this, x => x.BlocksToNextInstruction);
            MapService.CurrentRoute.WhenAnyValue(x => x.TotalBlocksRemaining)
                .Subscribe(Observer.Create<int>(remain =>
                {
                    BlocksToRouteEnd = $"{remain} blk";
                }));
            MapService.CurrentRoute.StartTrackingPlayer(MapService.LoggedInPlayer!);
        }
        else
        {
            MapService.CurrentRoute.StopTrackingPlayer();
        }
    }
}