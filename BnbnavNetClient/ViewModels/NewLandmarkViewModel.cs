using ReactiveUI.Fody.Helpers;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace BnbnavNetClient.ViewModels;
public class NewLandmarkViewModel : ViewModel
{
    [Reactive]
    public string LandmarkName { get; set; } = string.Empty;

    public string Title { get; }
    public string Watermark { get; }

    public ReactiveCommand<Unit, Unit> Cancel { get; }
    public ReactiveCommand<Unit, string> Ok { get; }

    public NewLandmarkViewModel(string title, string watermark)
    {
        Title = title;
        Watermark = watermark;
        Cancel = ReactiveCommand.Create(() => { });
        var isOk = this.WhenAnyValue(me => me.LandmarkName, s => !string.IsNullOrWhiteSpace(s));
        Ok = ReactiveCommand.Create(() => LandmarkName, isOk);
    }

    public NewLandmarkViewModel()
    {
        Title = "Enter something here:";
        Watermark = "Watermark";
        Cancel = ReactiveCommand.Create(() => { });
        var isOk = this.WhenAnyValue(me => me.LandmarkName, s => !string.IsNullOrWhiteSpace(s));
        Ok = ReactiveCommand.Create(() => LandmarkName, isOk);
    }
}
