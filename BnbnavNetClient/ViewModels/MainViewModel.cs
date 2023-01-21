using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BnbnavNetClient.Models;
using BnbnavNetClient.I18Next.Services;
using Avalonia;
using BnbnavNetClient.Settings;

namespace BnbnavNetClient.ViewModels;

public sealed class MainViewModel : ViewModel
{

    [Reactive]
    public bool HighlightTurnRestrictionsEnabled { get; set; }

    [Reactive]
    public bool FollowMeEnabled { get; set; }

    [ObservableAsProperty]
    public string FollowMeText { get; }

    [Reactive]
    public string FollowMeUsername { get; set; } = string.Empty;

    [Reactive]
    public string? EditModeToken { get; set; }

    [Reactive]
    public ViewModel? Popup { get; set; }

    [Reactive]
    public MapViewModel? MapViewModel { get; private set; }

    [ObservableAsProperty]
    public string PanText { get; }

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
    public bool RoadControlsRequired => false;

    [ObservableAsProperty]
    public bool EditModeEnabled => false;
    
    public Interaction<bool, Unit>? AuthTokeInteraction { get; set; }

    public MapEditorService MapEditorService { get; set; }

    public MainViewModel()
    {
        MapEditorService = new();
        
        _settings = AvaloniaLocator.Current.GetRequiredService<ISettingsManager>();
        _tr = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();
        var followMeText = this
            .WhenAnyValue(me => me.FollowMeEnabled, me => me.FollowMeUsername)
            .Select(x => x.Item1 ? _tr["FOLLOWING", ("user", x.Item2)] : _tr["FOLLOW_ME"]);
        followMeText.ToPropertyEx(this, me => me.FollowMeText);
        FollowMeText = _tr["FOLLOW_ME"];
        PanText = "x = 0; y = 0";

        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Select)
            .ToPropertyEx(this, x => x.IsInSelectMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Join)
            .ToPropertyEx(this, x => x.IsInJoinMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode, x => x.EditModeEnabled).Select(x =>
        {
            if (!x.Item2) return false;
            return x.Item1 is EditModeControl.Join;
        }).ToPropertyEx(this, x => x.RoadControlsRequired);
        MapEditorService.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.EditModeEnabled);
    }

    public async Task InitMapService()
    {
        var mapService = await MapService.DownloadInitialMapAsync();
        MapEditorService.MapService = mapService;
        MapViewModel = new(mapService, this);
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
            catch (Exception ex)
            {
                interaction.SetOutput(null);
            }
        });
    }
    
    public void LanguageButtonPressed()
    {
        var languagePopup = new LanguageSelectViewModel();
        languagePopup.Ok.Subscribe(async lang =>
        {
            Popup = null;
            _settings.Settings.Language = lang.Name;
            await _settings.SaveAsync();
        });
        Popup = languagePopup;
    }

    Task<string> ShowAuthenticationPopup()
    {
        var cs = new TaskCompletionSource<string>();
        var editModePopup = new EnterPopupViewModel(_tr["EDITNAV_PROMPT"], _tr["EDITNAV_WATERMARK"]);
        editModePopup.Ok.Subscribe(token =>
        {
            EditModeToken = token;
            MapEditorService.EditModeEnabled = true;
            cs.SetResult(token);
            Popup = null;
        });
        editModePopup.Cancel.Subscribe(_ =>
        {
            EditModeToken = null;
            MapEditorService.EditModeEnabled = false;
            cs.SetException(new Exception());
            Popup = null;
        });
        Popup = editModePopup;
        return cs.Task;
    }

    public void EditModePressed()
    {
        //MapEditorService.EditModeEnabled = !MapEditorService.EditModeEnabled;
        if (!MapEditorService.EditModeEnabled)
        {
            if (EditModeToken is not null)
            {
                MapEditorService.EditModeEnabled = true;
                return;
            }
            ShowAuthenticationPopup();
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

    public void FollowMePressed()
    {
        //annoyingly the button sets this for us...  so we undo it first
        FollowMeEnabled = !FollowMeEnabled;
        if (!FollowMeEnabled)
        {
            var followMePopup = new EnterPopupViewModel(_tr["FOLLOW_ME_PROMPT"], _tr["FOLLOW_ME_WATERMARK"]);
            Observable.Merge(
                followMePopup.Ok,
                followMePopup.Cancel.Select(_ => (string?)null))
                .Take(1)
                .Subscribe(str =>
                {
                    if (str is null)
                    {
                        FollowMeEnabled = false;
                    }
                    else
                    {
                        FollowMeEnabled = true;
                        FollowMeUsername = str;
                    }
                    Popup = null;
                });
            Popup = followMePopup;
        }
        else
        { 
            FollowMeEnabled = false;
            FollowMeUsername = string.Empty;
        }
    }
}
