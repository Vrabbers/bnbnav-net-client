﻿using BnbnavNetClient.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BnbnavNetClient.Models;

namespace BnbnavNetClient.ViewModels;

public sealed class MainViewModel : ViewModel
{

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

    //TODO: make this better
    [ObservableAsProperty] 
    public bool IsInSelectMode => MapEditorService.CurrentEditMode == EditModeControl.Select;

    [ObservableAsProperty] 
    public bool IsInJoinMode => MapEditorService.CurrentEditMode == EditModeControl.Join;

    [ObservableAsProperty] 
    public bool IsInJoinTwoWayMode => MapEditorService.CurrentEditMode == EditModeControl.JoinTwoWay;

    [ObservableAsProperty] 
    public bool RoadControlsRequired => false;

    [ObservableAsProperty]
    public bool EditModeEnabled => false;
    
    public MapEditorService MapEditorService { get; set; }

    public MainViewModel()
    {
        MapEditorService = new();
        
        var followMeText = this
            .WhenAnyValue(me => me.FollowMeEnabled, me => me.FollowMeUsername)
            .Select(x => x.Item1 ? $"Following {x.Item2}" : "Follow Me");
        followMeText.ToPropertyEx(this, me => me.FollowMeText);

        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Select)
            .ToPropertyEx(this, x => x.IsInSelectMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.Join)
            .ToPropertyEx(this, x => x.IsInJoinMode);
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode).Select(x => x == EditModeControl.JoinTwoWay)
            .ToPropertyEx(this, x => x.IsInJoinTwoWayMode); 
        MapEditorService.WhenAnyValue(x => x.CurrentEditMode, x => x.EditModeEnabled).Select(x =>
        {
            if (!x.Item2) return false;
            return x.Item1 is EditModeControl.Join or EditModeControl.JoinTwoWay;
        }).ToPropertyEx(this, x => x.RoadControlsRequired);
        MapEditorService.WhenAnyValue(x => x.EditModeEnabled).ToPropertyEx(this, x => x.EditModeEnabled);
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
            MapEditorService.EditModeEnabled = true;
            Popup = null;
        });
        editModePopup.Cancel.Subscribe(_ =>
        {
            MapEditorService.EditModeEnabled = false;
            Popup = null;
        });
        Popup = editModePopup;
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

    public void JoinTwoWayModePressed()
    {
        MapEditorService.CurrentEditMode = EditModeControl.JoinTwoWay;
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
