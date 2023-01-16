using BnbnavNetClient.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.Services;

public class MapEditorService : ReactiveObject
{
    enum ClickAction
    {
        Pan,
        JoinRoad
    }
    
    [Reactive]
    public EditModeControl CurrentEditMode { get; set; } = EditModeControl.Select;
    
    [Reactive]
    public bool EditModeEnabled { get; set; }
}