using Avalonia;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using BnbnavNetClient.Extensions;
using JetBrains.Annotations;
using Splat;

namespace BnbnavNetClient.ViewModels;
public sealed class LanguageSelectViewModel : ViewModel
{
    readonly IAvaloniaI18Next _tr;

    public IEnumerable<LanguageSelection> AvailableLanguages => 
        _tr.AvailableLanguages.Select(info => new LanguageSelection(info));

    [Reactive]
    public LanguageSelection ChosenLanguage { get; set; }

    public ReactiveCommand<Unit, CultureInfo> Ok { get; }

    [ObservableAsProperty]
    [UsedImplicitly]
    public bool LangChanged { get; }

    public LanguageSelectViewModel()
    {
        var settings = Locator.Current.GetSettingsManager();
        var presentLanguage = new CultureInfo(settings.Settings.Language);
        _tr = Locator.Current.GetI18Next();
        ChosenLanguage = new LanguageSelection(presentLanguage);
        Ok = ReactiveCommand.Create(() => ChosenLanguage.Info);
        this.WhenAnyValue(me => me.ChosenLanguage)
            .Select(chosen => chosen.Info.Name != presentLanguage.Name)
            .ToPropertyEx(this, me => me.LangChanged);
    }
}
public readonly record struct LanguageSelection(CultureInfo Info)
{
    public string PrettyPrint => $"{Info.Name} – {Info.NativeName} ({Info.DisplayName})";
}