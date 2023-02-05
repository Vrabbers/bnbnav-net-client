using System;
using System.Linq;
using System.Reactive;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using BnbnavNetClient.Services.NetworkOperations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public class LandmarkTypeHelper
{
    public LandmarkType LandmarkType { get; }

    public LandmarkTypeHelper(LandmarkType landmarkType)
    {
        LandmarkType = landmarkType;
    }

    public string HumanReadableName => LandmarkType.HumanReadableName();
}

public class LandmarkFlyoutViewModel : ViewModel, IOpenableAsFlyout
{
    readonly MapEditorService _mapEditorService;
    readonly Node _node;
    public FlyoutBase? Flyout { get; set; }
    
    [Reactive]
    public int CurrentTabIndex { get; set; }
    
    [Reactive]
    public bool SaveButtonEnabled { get; set; }

    public bool DeleteButtonEnabled => ExistingLandmark is not null;
    
    public Landmark? ExistingLandmark { get; }

    [Reactive]
    public string LandmarkName { get; set; } = "";
    
    [Reactive]
    public AvaloniaList<LandmarkTypeHelper> LandmarkTypes { get; set; } = new();

    [Reactive]
    public LandmarkTypeHelper? SelectedLandmarkType { get; set; }

    [Reactive]
    public string LabelName { get; set; } = "";
    
    [Reactive]
    public AvaloniaList<LandmarkTypeHelper> LabelTypes { get; set; } = new();

    [Reactive]
    public LandmarkTypeHelper? SelectedLabelType { get; set; }

    public LandmarkFlyoutViewModel(MapEditorService mapEditorService, Node node)
    {
        _mapEditorService = mapEditorService;
        _node = node;

        LandmarkTypes.AddRange(Enum.GetValues<LandmarkType>().Where(x => x.IsLandmark()).Select(x => new LandmarkTypeHelper(x)));
        LabelTypes.AddRange(Enum.GetValues<LandmarkType>().Where(x => x.IsLabel()).Select(x => new LandmarkTypeHelper(x)));
        
        ExistingLandmark = mapEditorService.MapService!.Landmarks.Values.FirstOrDefault(x => x.Node == node);
        if (ExistingLandmark is not null)
        {
            if (ExistingLandmark.LandmarkType.IsLabel())
            {
                CurrentTabIndex = 1;
                LabelName = ExistingLandmark.Name;
                SelectedLabelType = LabelTypes.SingleOrDefault(x => x.LandmarkType == ExistingLandmark.LandmarkType);
            }
            else
            {
                CurrentTabIndex = 0;
                LandmarkName = ExistingLandmark.Name;
                SelectedLandmarkType = LandmarkTypes.SingleOrDefault(x => x.LandmarkType == ExistingLandmark.LandmarkType);
            }
        }
        
        this.WhenAnyValue(x => x.CurrentTabIndex, x => x.LandmarkName, x => x.SelectedLandmarkType, x => x.LabelName, x => x.SelectedLabelType).Subscribe(
            Observer.Create<ValueTuple<int, string, LandmarkTypeHelper?, string, LandmarkTypeHelper?>>(_ =>
            {
                bool canCreate;
                if (CurrentTabIndex == 0)
                {
                    canCreate = SelectedLandmarkType is not null && !string.IsNullOrWhiteSpace(LandmarkName);
                }
                else
                { 
                    canCreate = SelectedLabelType is not null && !string.IsNullOrWhiteSpace(LabelName);
                }
                SaveButtonEnabled = canCreate;
            }));

    }

    public void CancelClicked()
    {
        Flyout?.Hide();
    }

    public void CommitClicked()
    {
        if (CurrentTabIndex == 0) //Landmark
        {
            _mapEditorService.TrackNetworkOperation(new LandmarkUpdateOperation(_mapEditorService, ExistingLandmark, new Landmark("temp", _node, LandmarkName, SelectedLandmarkType!.LandmarkType.ServerName())));
        }
        else
        {
            _mapEditorService.TrackNetworkOperation(new LandmarkUpdateOperation(_mapEditorService, ExistingLandmark, new Landmark("temp", _node, LabelName, SelectedLabelType!.LandmarkType.ServerName())));
        }
        Flyout?.Hide();
    }

    public void DeleteClicked()
    {
        _mapEditorService.TrackNetworkOperation(new LandmarkUpdateOperation(_mapEditorService, ExistingLandmark, null));
        Flyout?.Hide();
    }
}