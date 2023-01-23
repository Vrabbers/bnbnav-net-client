using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BnbnavNetClient.Views;

public partial class LanguageSelectView : UserControl
{
    public LanguageSelectView()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}