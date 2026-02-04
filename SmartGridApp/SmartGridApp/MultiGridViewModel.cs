using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartGridApp
{
    public partial class CategoryViewModel : ObservableObject
    {
        public string Name { get; set; } = "Category";
        public MainViewModel GridViewModel { get; } = new MainViewModel();

        [ObservableProperty]
        private int _visibleItemsCount;

        public CategoryViewModel(string name, int count)
        {
            Name = name;
            GridViewModel.CardCount = count;
        }
    }

    public partial class MultiGridViewModel : ObservableObject
    {
        public ObservableCollection<CategoryViewModel> Categories { get; } = new ObservableCollection<CategoryViewModel>();

        [ObservableProperty]
        private int _totalVisibleItems;

        [ObservableProperty]
        private int _totalCardCount;

        public MultiGridViewModel()
        {
            Categories.Add(new CategoryViewModel("Delicious Donuts", 40));
            Categories.Add(new CategoryViewModel("Sweet Treats", 60));
            Categories.Add(new CategoryViewModel("Glazed Goodness", 50));
            Categories.Add(new CategoryViewModel("Sprinkle Heaven", 80));
            Categories.Add(new CategoryViewModel("Choco Delights", 100));

            UpdateTotals();
            foreach (var cat in Categories)
            {
                cat.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(CategoryViewModel.VisibleItemsCount)) UpdateTotals(); };
                cat.GridViewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(MainViewModel.CardCount)) UpdateTotals(); };
            }
        }

        private void UpdateTotals()
        {
            int visible = 0;
            int total = 0;
            foreach (var cat in Categories)
            {
                visible += cat.VisibleItemsCount;
                total += cat.GridViewModel.CardCount;
            }
            TotalVisibleItems = visible;
            TotalCardCount = total;
        }
    }
}
