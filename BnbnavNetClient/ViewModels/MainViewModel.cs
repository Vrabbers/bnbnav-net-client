using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;
using System.Reactive.Linq;
using BnbnavNetClient.Models;
using BnbnavNetClient.I18Next.Services;
using Avalonia.Controls;
using BnbnavNetClient.Extensions;
using BnbnavNetClient.Settings;
using Splat;

namespace BnbnavNetClient.ViewModels;

public sealed class MainViewModel : ViewModel
{

    [Reactive]
    public bool HighlightTurnRestrictionsEnabled { get; set; }
    
    [Reactive]
    public bool HighlightInterWorldNodesEnabled { get; set; }

    [Reactive]
    public bool FollowMeEnabled { get; set; }

    [Reactive] public string ChosenWorld { get; set; } = null!;
    
    [ObservableAsProperty]
    public IEnumerable<string> AvailableWorlds { get; } = Enumerable.Empty<string>();

    [ObservableAsProperty]
    public string LoginText { get; }

    [Reactive]
    public string LoggedInUsername { get; set; }

    [Reactive]
    public string? EditModeToken { get; set; }

    [Reactive]
    public ViewModel? Popup { get; set; }

    [Reactive]
    public MapViewModel? MapViewModel { get; private set; }
    
    [Reactive]
    public CornerViewModel? CornerViewModel { get; private set; }
    
    public Button UserControlButton { get; set; } = null!;

    [ObservableAsProperty]
    public string PanText { get; }
    
    [ObservableAsProperty]
    public bool HaveLoggedInUser { get; set; }
    
    readonly IAvaloniaI18Next _tr;

    readonly ISettingsManager _settings;

    //TODO: make this better
    [ObservableAsProperty] 
    public bool IsInSelectMode => MapEditorService.CurrentEditMode == EditModeControl.Select;

    [ObservableAsProperty] 
    public bool IsInJoinMode => MapEditorService.CurrentEditMode == EditModeControl.Join;
    
    [ObservableAsProperty] 
    public bool IsInNodeMoveMode => MapEditorService.CurrentEditMode == EditModeControl.NodeMove;
    
    [ObservableAsProperty] 
    public bool IsInSpliceMode => MapEditorService.CurrentEditMode == EditModeControl.Splice;
    
    [ObservableAsProperty] 
    public bool IsInLandmarkMode => MapEditorService.CurrentEditMode == EditModeControl.Landmark;
    
    [ObservableAsProperty]
    public bool EditModeEnabled { get; }

    [Reactive]
    public bool MainBarVisible { get; set; } = true;

    public MapEditorService MapEditorService { get; set; }

    public MainViewModel()
    {
        MapEditorService = new MapEditorService();

        //TODO: Splat has a source generator for this. use it
        _settings = Locator.Current.GetSettingsManager();
        _tr = Locator.Current.GetI18Next();
        var followMeText = this
            .WhenAnyValue(me => me.FollowMeEnabled, me => me.LoggedInUsername)
            .Select(x => x.Item1 ? _tr["FOLLOWING", ("user", x.Item2)] : _tr["FOLLOW_ME"]);
        followMeText.ToPropertyEx(this, me => me.LoginText);
        LoginText = _tr["FOLLOW_ME"];
        PanText = "x = 0; y = 0";

        LoggedInUsername = _settings.Settings.LoggedInUser;

        this.WhenAnyValue(x => x.LoggedInUsername).Select(x => !string.IsNullOrEmpty(x))
            .ToPropertyEx(this, x => x.HaveLoggedInUser);

        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Select)
            .ToPropertyEx(this, x => x.IsInSelectMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Join)
            .ToPropertyEx(this, x => x.IsInJoinMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.NodeMove)
            .ToPropertyEx(this, x => x.IsInNodeMoveMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Splice)
            .ToPropertyEx(this, x => x.IsInSpliceMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Landmark)
            .ToPropertyEx(this, x => x.IsInLandmarkMode);
        MapEditorService.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.EditModeEnabled);
    }

    public async Task InitMapService()
    {
        var mapService = await MapService.DownloadInitialMapAsync();
        MapEditorService.MapService = mapService;

        ChosenWorld = mapService.Worlds.FirstOrDefault()!;
        
        this.WhenAnyValue(x => x.LoggedInUsername).ToPropertyEx(mapService, x => x.LoggedInUsername);
        mapService.WhenAnyValue(x => x.Worlds).ToPropertyEx(this, x => x.AvailableWorlds);

        MapViewModel = new MapViewModel(mapService, this);
        CornerViewModel = new CornerViewModel(mapService, this);
        
        var panText = MapViewModel
            .WhenAnyValue(map => map.Pan)
            .Select(pt => $"x = {double.Round(pt.X)}; y = {double.Round(pt.Y)}");
        panText.ToPropertyEx(this, me => me.PanText);

        this.WhenAnyValue(me => me.EditModeToken).Subscribe(token => MapService.AuthenticationToken = token);

        MapViewModel.MapService.AuthTokenInteraction.RegisterHandler(async interaction =>
        {
            try
            {
                var token = await ShowAuthenticationPopup();
                interaction.SetOutput(token);
            }
            catch (OperationCanceledException)
            {
                interaction.SetOutput(null);
            }
        });
        MapViewModel.MapService.ErrorMessageInteraction.RegisterHandler(interaction =>
        {
            var (title, message, terminateEditMode) = interaction.Input;

            if (terminateEditMode)
            {
                EditModeToken = null;
                MapEditorService.EditModeEnabled = false;
            }
            
            Popup = new AlertDialogViewModel()
            {
                Title = title,
                Message = message,
                Ok = ReactiveCommand.Create(() =>
                {
                    interaction.SetOutput(Unit.Default);
                    Popup = null;
                })
            };
            
            return Task.CompletedTask;
        });
        CornerViewModel.WhenAnyValue(x => x.SelectedLandmark).BindTo(MapViewModel, x => x.SelectedLandmark);
        CornerViewModel.WhenAnyValue(x => x.GoModeStartPoint).BindTo(MapViewModel, x => x.GoModeStartPoint);
        CornerViewModel.WhenAnyValue(x => x.GoModeEndPoint).BindTo(MapViewModel, x => x.GoModeEndPoint);
        CornerViewModel.WhenAnyValue(x => x.CurrentUi).BindTo(MapViewModel, x => x.CurrentUi);
        MapViewModel.WhenAnyValue(x => x.SelectedLandmark).BindTo(CornerViewModel, x => x.SelectedLandmark);
        MapViewModel.WhenAnyValue(x => x.GoModeStartPoint).BindTo(CornerViewModel, x => x.GoModeStartPoint);
        MapViewModel.WhenAnyValue(x => x.GoModeEndPoint).BindTo(CornerViewModel, x => x.GoModeEndPoint);
        MapViewModel.WhenAnyValue(x => x.CurrentUi).BindTo(CornerViewModel, x => x.CurrentUi);
        this.WhenAnyValue(x => x.HighlightInterWorldNodesEnabled)
            .BindTo(MapViewModel, x => x.HighlightInterWorldNodesEnabled);

        CornerViewModel.WhenAnyValue(x => x.CurrentUi).Subscribe(Observer.Create<AvailableUi>(currentUi =>
        {
            MainBarVisible = currentUi != AvailableUi.Go;
        }));
    }

    public void LanguageButtonPressed()
    {
        var languagePopup = new LanguageSelectViewModel();
        // ReSharper disable once AsyncVoidLambda
        languagePopup.Ok.Subscribe(async lang =>
        {
            Popup = null;
            _settings.Settings.Language = lang.Name;
            await _settings.SaveAsync();
        });
        Popup = languagePopup;
    }

    Task<string?> ShowAuthenticationPopup()
    {
        var cs = new TaskCompletionSource<string?>();
        var editModePopup = new EnterPopupViewModel(_tr["EDITNAV_PROMPT"], _tr["EDITNAV_WATERMARK"]);
        editModePopup.Ok.Subscribe(token =>
        {
            cs.SetResult(token);
            Popup = null;
        });
        editModePopup.Cancel.Subscribe(_ =>
        {
            cs.SetCanceled();
            Popup = null;
        });
        Popup = editModePopup;
        return cs.Task;
    }

    public async Task EditModePressed()
    {
        if (!MapEditorService.EditModeEnabled)
        {
            if (EditModeToken is not null)
            {
                MapEditorService.EditModeEnabled = true;
                return;
            }

            try
            {
                var auth = await ShowAuthenticationPopup();
                EditModeToken = auth;
                MapEditorService.EditModeEnabled = true;
            }
            catch (OperationCanceledException)
            {
                EditModeToken = null;
                MapEditorService.EditModeEnabled = false;
            }
            this.RaisePropertyChanged(nameof(EditModeEnabled));
        }
        else
        {
            MapEditorService.EditModeEnabled = false;
        }
    }

    public void SelectModePressed()
    {
        MapEditorService.CurrentEditMode = EditModeControl.Select;
    }

    public void JoinModePressed()
    {
        MapEditorService.CurrentEditMode = EditModeControl.Join;
    }
    
    public void NodeMovePressed()
    {
        MapEditorService.CurrentEditMode = EditModeControl.NodeMove;
    }
    
    public void SplicePressed()
    {
        MapEditorService.CurrentEditMode = EditModeControl.Splice;
    }

    public void LandmarkPressed()
    {
        MapEditorService.CurrentEditMode = EditModeControl.Landmark;
    }

    async Task SetLogin(string loggedInUsername)
    {
        LoggedInUsername = loggedInUsername;
        _settings.Settings.LoggedInUser = loggedInUsername;
        await _settings.SaveAsync();
    }

    public void LoginPressed()
    {
        var followMePopup = new EnterPopupViewModel(_tr["FOLLOW_ME_PROMPT"], _tr["FOLLOW_ME_WATERMARK"]);
        followMePopup.Ok.Merge(followMePopup.Cancel.Select(_ => (string?)null))
            .Take(1)
            // ReSharper disable once AsyncVoidLambda
            .Subscribe(async str =>
            {
                if (str is not null)
                {
                    await SetLogin(str);
                }
                Popup = null;
            });
        Popup = followMePopup;
    }

    public async Task LogoutPressed()
    {
        await SetLogin("");
        UserControlButton.Flyout!.Hide();
    }
}
