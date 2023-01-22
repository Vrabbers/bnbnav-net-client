using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace BnbnavNetClient.ViewModels;

public interface IOpenableAsFlyout
{
    public FlyoutBase Flyout { get; set; }
}