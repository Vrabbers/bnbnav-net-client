using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BnbnavNetClient.Views;

public partial class LandmarkFlyoutView : UserControl
{
    public LandmarkFlyoutView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}