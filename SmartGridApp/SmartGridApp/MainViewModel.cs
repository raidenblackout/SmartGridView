using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartGridApp
{
    public partial class MainViewModel : ObservableObject
    {
        public ObservableCollection<CardViewModel> Cards { get; } = new ObservableCollection<CardViewModel>();

        [ObservableProperty]
        private int _cardCount = 100;

        public MainViewModel()
        {
            UpdateCardCollection();
        }

        partial void OnCardCountChanged(int value)
        {
            UpdateCardCollection();
        }

        private void UpdateCardCollection()
        {
            if (CardCount < 0) return;

            // Simple diffing to avoid clearing the whole list
            while (Cards.Count > CardCount)
            {
                Cards.RemoveAt(Cards.Count - 1);
            }

            while (Cards.Count < CardCount)
            {
                int nextIndex = Cards.Count;
                Cards.Add(new CardViewModel
                {
                    Title = $"Card {nextIndex}",
                    DisplayIndex = nextIndex.ToString()
                });
            }
        }
    }
}
