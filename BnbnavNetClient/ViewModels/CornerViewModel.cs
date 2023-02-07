using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
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
    readonly IAvaloniaI18Next _i18N;
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

    CancellationTokenSource? RouteCalculationCancellationSource { get; set; }

    [Reactive]
    public bool CalculatingRoute { get; set; }

    [Reactive]
    public string? RouteCalculationError { get; set; }

    [Reactive]
    public string BlocksToRouteEnd { get; set; } = "0 blk";

    [ObservableAsProperty]
    public bool CurrentInstructionValid { get; }
    
    [Reactive]
    public bool AvoidTolls { get; set; }

    [Reactive]
    public bool AvoidMotorways { get; set; }
    
    [Reactive]
    public bool AvoidFerries { get; set; }
    
    [Reactive]
    public bool AvoidDuongWarp { get; set; }
    
    [Reactive]
    public bool IsMuteEnabled { get; set; }

    public CornerViewModel(MapService mapService, MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        MapService = mapService;
        _i18N = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();

        this.WhenAnyValue(x => x.CurrentUi).Subscribe(Observer.Create<AvailableUi>(_ =>
        {
            IsInSearchMode = CurrentUi == AvailableUi.Search;
            IsInPrepareMode = CurrentUi == AvailableUi.Prepare;
            IsInGoMode = CurrentUi == AvailableUi.Go;

            if (IsInSearchMode)
            {
                //There should never be a current route in search mode
                MapService.CurrentRoute?.Dispose();
                MapService.CurrentRoute = null;
            }
            
            SetupRouteForGoMode();
        }));
        this.WhenAnyValue(x => x.GoModeEndPoint).Subscribe(Observer.Create<ISearchable?>(_ =>
        {
            if (GoModeStartPoint is not null) 
                return;
            if (MapService.LoggedInPlayer is null) return;

            //Attempt to find the player
            GoModeStartPoint = MapService.LoggedInPlayer;
        }));
        this.WhenAnyValue(x => x.GoModeStartPoint, x => x.GoModeEndPoint, x => x.AvoidMotorways, x => x.AvoidDuongWarp,
            x => x.AvoidTolls, x => x.AvoidFerries).Subscribe(Observer.Create<
            // ReSharper disable once AsyncVoidLambda
            ValueTuple<ISearchable?, ISearchable?, bool, bool, bool, bool>>(async _ =>
                {
                    RouteCalculationCancellationSource?.Cancel();
                    RouteCalculationCancellationSource = new CancellationTokenSource();
                    
                    //Clear the current route
                    MapService.CurrentRoute?.Dispose();
                    MapService.CurrentRoute = null;
                    RouteCalculationError = null;
                    
                    if (GoModeStartPoint is null || GoModeEndPoint is null)
                    {
                        return;
                    }

                    await CalculateAndSetRoute();
                }));

        this.WhenAnyValue(x => x.CalculatingRoute, x => x.RouteCalculationError)
            .Select(tuple => !tuple.Item1 && string.IsNullOrEmpty(tuple.Item2))
            .ToPropertyEx(this, x => x.CurrentInstructionValid);

        this.WhenAnyValue(x => x.IsMuteEnabled).Subscribe(Observer.Create<bool>(mute =>
        {
            if (MapService.CurrentRoute is not null)
            {
                MapService.CurrentRoute.Mute = mute;
            }
        }));

        MapService.WhenAnyValue(x => x.LoggedInPlayer).Subscribe(Observer.Create<Player?>(player =>
        {
            if (player is null && IsInGoMode)
            {
                //Immediately quit Go Mode as the player has disconnected
                CurrentUi = AvailableUi.Prepare;
                    
                _mainViewModel.Popup = new AlertDialogViewModel()
                {
                    Title = _i18N["GO_MODE_PLAYER_DISCONNECTED_TITLE"],
                    Message = _i18N["GO_MODE_PLAYER_DISCONNECTED_MESSAGE"],
                    Ok = ReactiveCommand.Create(() =>
                    {
                        _mainViewModel.Popup = null;
                    })
                };
            }
        }));
    }

    public async Task CalculateAndSetRoute()
    {
        RouteCalculationCancellationSource?.Cancel();
        RouteCalculationCancellationSource = new CancellationTokenSource();

        if (GoModeStartPoint is null || GoModeEndPoint is null) return;
        
        try
        {
            CalculatingRoute = true;

            var routeOptions = MapService.RouteOptions.NoRouteOptions;
            if (AvoidMotorways) routeOptions |= MapService.RouteOptions.AvoidMotorways;
            if (AvoidDuongWarp) routeOptions |= MapService.RouteOptions.AvoidDuongWarp;
            if (AvoidTolls) routeOptions |= MapService.RouteOptions.AvoidTolls;
            if (AvoidFerries) routeOptions |= MapService.RouteOptions.AvoidFerries;
            
            var route = await MapService.ObtainCalculatedRoute(GoModeStartPoint, GoModeEndPoint, routeOptions,
                RouteCalculationCancellationSource.Token);
            MapService.CurrentRoute = route;
            MapService.CurrentRoute.Mute = IsMuteEnabled;
            CalculatingRoute = false;
            
            RouteCalculationCancellationSource = null;

            foreach (var inst in route.Instructions)
            {
                Console.WriteLine(inst.HumanReadableString((int)inst.Distance));
            }
        }
        catch (NoSuitableEdgeException)
        {
            RouteCalculationCancellationSource = null;
            CalculatingRoute = false;
            RouteCalculationError = _i18N["DIRECTIONS_CALCULATING_FAILURE_NO_ROAD"];
        }
        catch (DisjointNetworkException)
        {
            RouteCalculationCancellationSource = null;
            CalculatingRoute = false;
            RouteCalculationError = _i18N["DIRECTIONS_CALCULATING_FAILURE_NO_PATH"];
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    void GoModeRerouteRequested(object? sender, EventArgs e)
    {
        // ReSharper disable once AsyncVoidLambda
        Dispatcher.UIThread.Post(async () =>
        {
            //Clear the current route
            MapService.CurrentRoute?.Dispose();
            MapService.CurrentRoute = null;
            RouteCalculationError = null;
            
            await CalculateAndSetRoute();
            SetupRouteForGoMode();
        });
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
        if (MapService.LoggedInPlayer is null)
        {
            if (string.IsNullOrEmpty(MapService.LoggedInUsername))
            {
                //TODO: Ask the player to log in
            }
            else
            {
                _mainViewModel.Popup = new AlertDialogViewModel()
                {
                    Title = _i18N["GO_MODE_START_ERROR_TITLE"],
                    Message = _i18N["GO_MODE_START_ERROR_LOGIN_REQUIRED"],
                    Ok = ReactiveCommand.Create(() =>
                    {
                        _mainViewModel.Popup = null;
                    })
                };
            }

            return;
        }

        if (MapService.LoggedInPlayer != GoModeStartPoint)
        {
            _mainViewModel.Popup = new AlertDialogViewModel()
            {
                Title = _i18N["GO_MODE_START_ERROR_TITLE"],
                Message = _i18N["GO_MODE_START_ERROR_INVALID_START_POINT"],
                Ok = ReactiveCommand.Create(() =>
                {
                    _mainViewModel.Popup = null;
                })
            };

            return;
        }
        
        _mainViewModel.Popup = new AlertDialogViewModel()
        {
            Title = _i18N["GO_MODE_WARNING_TITLE"],
            Message = _i18N["GO_MODE_WARNING_MESSAGE"],
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

        if (IsInGoMode && MapService.LoggedInPlayer is not null)
        {
            MapService.CurrentRoute.WhenAnyValue(x => x.TotalBlocksRemaining)
                // ReSharper disable once AsyncVoidLambda
                .Subscribe(Observer.Create<int>(remain =>
                {
                    BlocksToRouteEnd = $"{remain} blk";
                    if (remain is < 20 and > 1)
                    {
                        // ReSharper disable once AsyncVoidLambda
                        Dispatcher.UIThread.Post(async () =>
                        {
                            //End nav
                            await Task.Delay(3000);
                            var landmark = GoModeEndPoint;
                            LeavePrepareMode();

                            SelectedLandmark = null;
                            SelectedLandmark = landmark;
                        });
                    }
                }));
            MapService.CurrentRoute.StartTrackingPlayer(MapService.LoggedInPlayer);
            MapService.CurrentRoute.RerouteRequested += GoModeRerouteRequested;
        }
        else
        {
            MapService.CurrentRoute.StopTrackingPlayer();
            MapService.CurrentRoute.RerouteRequested -= GoModeRerouteRequested;
        }
    }
}