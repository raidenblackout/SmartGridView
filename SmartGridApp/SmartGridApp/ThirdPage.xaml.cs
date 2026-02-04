using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmartGridApp
{
    public sealed partial class ThirdPage : Page
    {
        public MultiGridViewModel ViewModel { get; } = new MultiGridViewModel();

        public ThirdPage()
        {
            this.InitializeComponent();
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
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
}
