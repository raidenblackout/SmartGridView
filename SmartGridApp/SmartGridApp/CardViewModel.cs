using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartGrid.Core;

namespace SmartGridApp
{
    public partial class CardViewModel : ObservableObject, IExpandableItem
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _displayIndex = string.Empty;

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public event EventHandler<EventArgs>? ToggleRequested;

        [RelayCommand]
        private void RequestToggle()
        {
            ToggleRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
