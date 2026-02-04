using Microsoft.UI.Xaml.Controls;

namespace SmartGridApp;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel => App.MainPageViewModel;

    public MainPage()
    {
        this.InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
    }

    private void OnNavigateClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        this.Frame.Navigate(typeof(SecondPage));
    }

    private void OnExpandClicked(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement fe && fe.DataContext is CardViewModel vm)
        {
            // Trigger the command which fires the ToggleRequested event monitored by SmartGridView
            if (vm.RequestToggleCommand.CanExecute(null))
            {
                vm.RequestToggleCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    private void OnCardSelected(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement fe && fe.DataContext is CardViewModel vm)
        {
            vm.IsSelected = !vm.IsSelected;
            e.Handled = true;
        }
    }
}
