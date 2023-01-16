using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BnbnavNetClient.Views;

public partial class AlertDialogView : UserControl
{
    public AlertDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}