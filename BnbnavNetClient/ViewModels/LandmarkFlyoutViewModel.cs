using Avalonia.Controls.Primitives;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;

namespace BnbnavNetClient.ViewModels;

public class LandmarkFlyoutViewModel : ViewModel, IOpenableAsFlyout
{
    private readonly MapEditorService _mapEditorService;
    private readonly Node _node;
    public FlyoutBase Flyout { get; set; }

    public LandmarkFlyoutViewModel(MapEditorService mapEditorService, Node node)
    {
        _mapEditorService = mapEditorService;
        _node = node;
    }
}