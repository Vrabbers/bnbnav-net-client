using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

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

    public MainViewModel()
    {
        var followMeText = this
            .WhenAnyValue(me => me.FollowMeEnabled, me => me.FollowMeUsername)
            .Select(x => x.Item1 ? $"Following {x.Item2}" : "Follow Me");
        followMeText.ToPropertyEx(this, me => me.FollowMeText);
    }

    public async Task InitMapService()
    {
        var mapService = await MapService.DownloadInitialMapAsync();
        MapViewModel = new(mapService, this);
        var panText = MapViewModel
            .WhenAnyValue(map => map.Pan)
            .Select(pt => $"x = {double.Round(pt.X)}; y = {double.Round(pt.Y)}");
        panText.ToPropertyEx(this, me => me.PanText);
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
        else
        {
            EditModeEnabled = false;
        }
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
