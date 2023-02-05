using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BnbnavNetClient.Views;

public partial class RoadEditView : UserControl
{
    public RoadEditView()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}