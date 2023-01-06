using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;

namespace BnbnavNetClient.ViewModels;
public sealed class EnterPopupViewModel : ViewModel
{
    [Reactive]
    public string Input { get; set; } = string.Empty;

    public string Title { get; }
    public string Watermark { get; }

    public ReactiveCommand<Unit, Unit> Cancel { get; }
    public ReactiveCommand<Unit, string> Ok { get; }

    public EnterPopupViewModel(string title, string watermark)
    {
        Title = title;
        Watermark = watermark;
        Cancel = ReactiveCommand.Create(() => { });
        var isOk = this.WhenAnyValue(me => me.Input, s => !string.IsNullOrWhiteSpace(s));
        Ok = ReactiveCommand.Create(() => Input, isOk);
    }

    public EnterPopupViewModel()
    {
        Title = "Enter something here:";
        Watermark = "Watermark";
        Cancel = ReactiveCommand.Create(() => { });
        var isOk = this.WhenAnyValue(me => me.Input, s => !string.IsNullOrWhiteSpace(s));
        Ok = ReactiveCommand.Create(() => Input, isOk);
    }
}
