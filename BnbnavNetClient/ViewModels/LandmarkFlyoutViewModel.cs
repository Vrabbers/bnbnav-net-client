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
    private readonly MapEditorService _mapEditorService;
    private readonly Node _node;
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

    public LandmarkFlyoutViewModel(MapEditorService mapEditorService, Node node)
    {
        _mapEditorService = mapEditorService;
        _node = node;

        LandmarkTypes.AddRange(Enum.GetValues<LandmarkType>().Skip(1).Select(x => new LandmarkTypeHelper(x)));
        
        ExistingLandmark = mapEditorService.MapService!.Landmarks.Values.FirstOrDefault(x => x.Node == node);
        if (ExistingLandmark is not null)
        {
            LandmarkName = ExistingLandmark.Name;
            SelectedLandmarkType = LandmarkTypes.SingleOrDefault(x => x.LandmarkType == ExistingLandmark.LandmarkType);
        }
        
        this.WhenAnyValue(x => x.CurrentTabIndex, x => x.LandmarkName, x => x.SelectedLandmarkType).Subscribe(
            Observer.Create<ValueTuple<int, string, LandmarkTypeHelper?>>(tuple =>
            {
                var canCreate = true;
                if (CurrentTabIndex == 0)
                {
                    canCreate = SelectedLandmarkType is not null && !string.IsNullOrWhiteSpace(LandmarkName);
                }
                else
                { 
                    canCreate = false;
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
            _mapEditorService.TrackNetworkOperation(new LandmarkUpdateOperation(_mapEditorService, ExistingLandmark, new("temp", _node, LandmarkName, SelectedLandmarkType!.LandmarkType.ServerName())));
        }
        Flyout?.Hide();
    }

    public void DeleteClicked()
    {
        _mapEditorService.TrackNetworkOperation(new LandmarkUpdateOperation(_mapEditorService, ExistingLandmark, null));
        Flyout?.Hide();
    }
}