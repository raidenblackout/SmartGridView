using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace SmartGridApp
{
    public partial class MainViewModel : ObservableObject
    {
        private static readonly string DonutUrl = "https://images.vexels.com/media/users/3/185202/isolated/preview/04210f166dee214fc751791106b453b2-donut-pink-syrup-icon.png";

        // Shared ImageSource to eliminate flashing in virtualized grid
        private static ImageSource? _sharedDonutSource;
        private static ImageSource SharedDonutSource
        {
            get
            {
                if (_sharedDonutSource == null)
                {
                    var bitmap = new BitmapImage(new Uri(DonutUrl));
                    // Optimize decoder size for the 240x240 cards
                    bitmap.DecodePixelWidth = 240;
                    _sharedDonutSource = bitmap;
                }
                return _sharedDonutSource;
            }
        }

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
                    Title = $"Donut {nextIndex}",
                    DisplayIndex = nextIndex.ToString(),
                    ImageUrl1 = SharedDonutSource,
                    ImageUrl2 = SharedDonutSource
                });
            }
        }
    }
}
