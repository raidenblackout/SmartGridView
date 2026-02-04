using Microsoft.UI.Xaml.Controls;

namespace SmartGridApp;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new MainViewModel();

    public MainPage()
    {
        this.InitializeComponent();
    }
}
