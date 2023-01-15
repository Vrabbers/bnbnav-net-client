using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BnbnavNetClient.Models;

namespace BnbnavNetClient.ViewModels;

public sealed class MainViewModel : ViewModel
{
    [Reactive]
    public bool EditModeEnabled { get; set; }

    [Reactive]
    public bool HighlightTurnRestrictionsEnabled { get; set; }

    [Reactive]
    public bool FollowMeEnabled { get; set; }

    [ObservableAsProperty]
    public string FollowMeText { get; } = "Follow Me";

    [Reactive]
    public string FollowMeUsername { get; set; } = string.Empty;

    [Reactive]
    public string? EditModeToken { get; set; }

    [Reactive]
    public ViewModel? Popup { get; set; }

    [Reactive]
    public MapViewModel? MapViewModel { get; private set; }

    [ObservableAsProperty]
    public string PanText { get; } = "x = 0; y = 0";

    [Reactive]
    public EditModeControl CurrentEditMode { get; set; } = EditModeControl.Select;

    //TODO: make this better
    [ObservableAsProperty] 
    public bool IsInSelectMode => CurrentEditMode == EditModeControl.Select;

    [ObservableAsProperty] 
    public bool IsInJoinMode => CurrentEditMode == EditModeControl.Join;

    [ObservableAsProperty] 
    public bool IsInJoinTwoWayMode => CurrentEditMode == EditModeControl.JoinTwoWay;

    [ObservableAsProperty] 
    public bool RoadControlsRequired => false;
    

    public MainViewModel()
    {
        var followMeText = this
            .WhenAnyValue(me => me.FollowMeEnabled, me => me.FollowMeUsername)
            .Select(x => x.Item1 ? $"Following {x.Item2}" : "Follow Me");
        followMeText.ToPropertyEx(this, me => me.FollowMeText);

        this.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Select)
            .ToPropertyEx(this, x => x.IsInSelectMode);
        this.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Join)
            .ToPropertyEx(this, x => x.IsInJoinMode);
        this.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.JoinTwoWay)
            .ToPropertyEx(this, x => x.IsInJoinTwoWayMode);
        this.WhenAnyValue(x => x.CurrentEditMode, x => x.EditModeEnabled).Select(x =>
        {
            if (!x.Item2) return false;
            return x.Item1 is EditModeControl.Join or EditModeControl.JoinTwoWay;
        }).ToPropertyEx(this, x => x.RoadControlsRequired);
    }

    public async Task InitMapService()
    {
        var mapService = await MapService.DownloadInitialMapAsync();
        MapViewModel = new(mapService, this);
        var panText = MapViewModel
            .WhenAnyValue(map => map.Pan)
            .Select(pt => $"x = {double.Round(pt.X)}; y = {double.Round(pt.Y)}");
        panText.ToPropertyEx(this, me => me.PanText);

        this.WhenAnyValue(me => me.EditModeToken).Subscribe(token => MapService.AuthenticationToken = token);

        MapViewModel.MapService.AuthTokenInteraction.RegisterHandler(async interaction => 
        {
            ShowAuthenticationPopup();
            var token = await this.WhenAnyValue(me => me.EditModeToken);
            interaction.SetOutput(token);
        });
    }

    void ShowAuthenticationPopup()
    {
        var editModePopup = new EnterPopupViewModel("Use /editnav to obtain a token and enter here:", "Token");
        editModePopup.Ok.Subscribe(token =>
        {
            EditModeToken = token;
            EditModeEnabled = true;
            Popup = null;
        });
        editModePopup.Cancel.Subscribe(_ =>
        {
            EditModeEnabled = false;
            Popup = null;
        });
        Popup = editModePopup;
    }

    public void EditModePressed()
    {
        EditModeEnabled = !EditModeEnabled;
        if(!EditModeEnabled)
        {
            if (EditModeToken is not null)
            {
                EditModeEnabled = true;
                return;
            }
            ShowAuthenticationPopup();
        }
        else
        {
            EditModeEnabled = false;
        }
    }

    public void SelectModePressed()
    {
        CurrentEditMode = EditModeControl.Select;
    }

    public void JoinModePressed()
    {
        CurrentEditMode = EditModeControl.Join;
    }

    public void JoinTwoWayModePressed()
    {
        CurrentEditMode = EditModeControl.JoinTwoWay;
    }

    public void FollowMePressed()
    {
        //annoyingly the button sets this for us...  so we undo it first
        FollowMeEnabled = !FollowMeEnabled;
        if (!FollowMeEnabled)
        {
            var followMePopup = new EnterPopupViewModel("Enter Minecraft username:", "Username");
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
