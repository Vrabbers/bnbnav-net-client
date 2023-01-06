using Avalonia.Controls;
using Avalonia.Interactivity;
using BnbnavNetClient.Services;
using BnbnavNetClient.ViewModels;
using System;
using System.Threading.Tasks;

namespace BnbnavNetClient.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public async void ViewLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        await vm.InitMapService();
    }
}