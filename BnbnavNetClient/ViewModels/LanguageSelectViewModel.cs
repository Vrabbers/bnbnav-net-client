using Avalonia;
using BnbnavNetClient.I18Next.Services;
using BnbnavNetClient.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reactive;

namespace BnbnavNetClient.ViewModels;
public class LanguageSelectViewModel : ViewModel
{
    readonly IAvaloniaI18Next _tr;

    public IEnumerable<CultureInfo> AvailableLanguages => _tr.AvailableLanguages;

    [Reactive]
    public CultureInfo ChosenLanguage { get; set; }

    public ReactiveCommand<Unit, CultureInfo> Ok { get; }

    public LanguageSelectViewModel()
    {
        _tr = AvaloniaLocator.Current.GetRequiredService<IAvaloniaI18Next>();
        ChosenLanguage = new(SettingsManager.Settings.Language);
        Ok = ReactiveCommand.Create(() => ChosenLanguage);
    }
}
