using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartGridApp
{
    public sealed partial class SecondPage : Page
    {
        public MainViewModel ViewModel => App.SecondPageViewModel;

        public SecondPage()
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

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private void OnExpandClicked(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.FrameworkElement fe && fe.DataContext is CardViewModel vm)
            {
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
}
