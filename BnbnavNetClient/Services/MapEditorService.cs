using System;
using System.Collections.Generic;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services.EditControllers;
using BnbnavNetClient.Services.NetworkOperations;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.Services;

public class MapEditorService : ReactiveObject
{
    List<NetworkOperation> _networkOperations = new();
    
    public MapEditorService()
    {
        EditController = new SelectEditController(this);
        this.ObservableForProperty(x => x.CurrentEditMode).Subscribe(x =>
        {
            EditController = x.Value switch
            {
                EditModeControl.Select => new SelectEditController(this),
                EditModeControl.Join => new NodeJoinEditController(this),
                EditModeControl.NodeMove => new NodeMoveEditController(this),
                EditModeControl.Splice => new SpliceEditController(this),
                EditModeControl.Landmark => new LandmarkEditController(this),
                _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
            };
        });
    }

    public void TrackNetworkOperation(NetworkOperation operation)
    {
        _networkOperations.Add(operation);
        operation.PerformOperation().ContinueWith(_ =>
        {
            _networkOperations.Remove(operation);
            this.RaisePropertyChanged(nameof(OngoingNetworkOperations));
        });
        this.RaisePropertyChanged(nameof(OngoingNetworkOperations));
    }

    [Reactive]
    public EditModeControl CurrentEditMode { get; set; } = EditModeControl.Select;

    [Reactive]
    public bool EditModeEnabled { get; set; }
    
    public EditController EditController { get; private set; }
    
    public MapService? MapService { get; set; }

    public IReadOnlyList<NetworkOperation> OngoingNetworkOperations => _networkOperations.AsReadOnly();
}