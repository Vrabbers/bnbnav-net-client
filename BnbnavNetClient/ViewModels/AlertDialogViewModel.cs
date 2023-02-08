using System.Reactive;
using ReactiveUI;

namespace BnbnavNetClient.ViewModels;

public sealed class AlertDialogViewModel : ViewModel
{
    public AlertDialogViewModel()
    {
        Ok = ReactiveCommand.Create(() => { });
    }

    public string? Title { get; init; }
    public string? Message { get; init; }
    public ReactiveCommand<Unit, Unit> Ok { get; init; }
}