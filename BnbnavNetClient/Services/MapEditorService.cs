using System;
using System.Reactive.Linq;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services.EditControllers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.Services;

public class MapEditorService : ReactiveObject
{
    public MapEditorService()
    {
        EditController = new SelectEditController();
        this.ObservableForProperty(x => x.CurrentEditMode).Subscribe(x =>
        {
            EditController = x.Value switch
            {
                EditModeControl.Select => new SelectEditController(),
                EditModeControl.Join => new NodeJoinEditController(this),
                EditModeControl.NodeMove => new NodeMoveEditController(),
                _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
            };
        });
    }

    enum ClickAction
    {
        Pan,
        JoinRoad
    }

    [Reactive]
    public EditModeControl CurrentEditMode { get; set; } = EditModeControl.Select;

    [Reactive]
    public bool EditModeEnabled { get; set; }
    
    public IEditController EditController { get; private set; }
    
    public MapService? MapService { get; set; }
}