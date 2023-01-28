using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BnbnavNetClient.Models;
using BnbnavNetClient.Services;
using BnbnavNetClient.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BnbnavNetClient.Views;

public partial class LandmarkSearchView : UserControl
{
    Landmark? _SelectedLandmark;
    public static readonly DirectProperty<LandmarkSearchView, Landmark?> SelectedLandmarkProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchView, Landmark?>("SelectedLandmark", o => o.SelectedLandmark, (o, v) => o.SelectedLandmark = v);
    
    MapService _MapService = null!;
    public static readonly DirectProperty<LandmarkSearchView, MapService> MapServiceProperty = AvaloniaProperty.RegisterDirect<LandmarkSearchView, MapService>("MapService", o => o.MapService, (o, v) => o.MapService = v);

    public LandmarkSearchView()
    {
        DataContext = new LandmarkSearchControlViewModel();
        InitializeComponent();
    }

    public Landmark? SelectedLandmark
    {
        get { return _SelectedLandmark; }
        set { SetAndRaise(SelectedLandmarkProperty, ref _SelectedLandmark, value); }
    }

    public MapService MapService
    {
        get { return _MapService; }
        set { SetAndRaise(MapServiceProperty, ref _MapService, value); }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
    }
}