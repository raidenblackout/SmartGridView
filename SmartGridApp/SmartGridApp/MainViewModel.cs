using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartGridApp
{
    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<CardViewModel> Cards { get; } = new ObservableCollection<CardViewModel>();

        public MainViewModel()
        {
            for (int i = 0; i < 100; i++)
            {
                Cards.Add(new CardViewModel { Title = $"Card {i}", DisplayIndex = i.ToString() });
            }
        }
    }
}
