using System.Linq;
using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.ViewModels;

public class LandmarkFlyoutViewModel : ViewModel, IOpenableAsFlyout
{
    private readonly MapEditorService _mapEditorService;
    private readonly Node _node;
    public FlyoutBase? Flyout { get; set; }
    
    [Reactive]
    public int CurrentTabIndex { get; set; }
    
    public bool CommitButtonEnabled => true;

    public bool DeleteButtonEnabled => ExistingLandmark is not null;
    
    public Landmark? ExistingLandmark { get; init; }

    public LandmarkFlyoutViewModel(MapEditorService mapEditorService, Node node)
    {
        _mapEditorService = mapEditorService;
        _node = node;

        ExistingLandmark = mapEditorService.MapService!.Landmarks.Values.FirstOrDefault(x => x.Node == node);
    }

    public void CancelClicked()
    {
        Flyout?.Hide();
    }

    public void CommitClicked()
    {
        Flyout?.Hide();
    }

    public void DeleteClicked()
    {
        
    }
}