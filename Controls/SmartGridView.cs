using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartGrid.Core;
using SmartGrid.Views;

namespace SmartGrid.Controls
{
    public sealed class SmartGridView : Control
    {
        private ItemsRepeater _repeater;
        private FastCardLayout _layout;
        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _navigatingCts;

        public SmartGridView()
        {
            this.DefaultStyleKey = typeof(SmartGridView);
            _layout = new FastCardLayout();
            
            // Subscribe to layout's request for refresh (e.g. during drag operation)
            _layout.RequestLayoutRefresh += async (s, args) => 
            {
               await RunLayoutUpdateAsync(args.dragIdx, args.targetIdx);
            };

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            
            // Enable Drop on the Grid itself
            this.AllowDrop = true;
            this.DragOver += OnDragOver;
            this.Drop += OnDrop;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(SmartGridView), new PropertyMetadata(null));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(SmartGridView), 
            new PropertyMetadata(null, OnItemsSourceChanged));

        public object ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SmallCardSizeProperty =
            DependencyProperty.Register(nameof(SmallCardSize), typeof(Size), typeof(SmartGridView), new PropertyMetadata(new Size(160, 160)));

        public Size SmallCardSize
        {
            get => (Size)GetValue(SmallCardSizeProperty);
            set => SetValue(SmallCardSizeProperty, value);
        }

        public static readonly DependencyProperty LargeCardSizeProperty =
            DependencyProperty.Register(nameof(LargeCardSize), typeof(Size), typeof(SmartGridView), new PropertyMetadata(new Size(320, 320)));

        public Size LargeCardSize
        {
            get => (Size)GetValue(LargeCardSizeProperty);
            set => SetValue(LargeCardSizeProperty, value);
        }

        #endregion


        protected override void OnApplyTemplate()
        {
            _repeater = GetTemplateChild("PART_Repeater") as ItemsRepeater;
            if (_repeater != null)
            {
                _repeater.Layout = _layout;
                
                // Subscribe to element prepared for DragStarting setup
                _repeater.ElementPrepared += OnElementPrepared;
            }
            base.OnApplyTemplate();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initial layout calculation if needed
             if (ItemsSource is IEnumerable<IExpandableItem> items)
             {
                 _ = RecalculateLayoutAsync(items);
             }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _navigatingCts?.Cancel();
            // Clean up event subscriptions if necessary, but keep layout cache for re-entry
        }



        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = (SmartGridView)d;
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += grid.OnCollectionChanged;
            }
             if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= grid.OnCollectionChanged;
            }

            if (e.NewValue is IEnumerable<IExpandableItem> items)
            {
                foreach (var item in items) 
                {
                    // Minimize duplicate subscriptions
                    item.ToggleRequested -= grid.OnItemToggleRequested;
                    item.ToggleRequested += grid.OnItemToggleRequested;
                }
                _ = grid.RecalculateLayoutAsync(items);
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle new items -> subscribe events
            if (e.NewItems != null)
            {
                foreach (IExpandableItem item in e.NewItems) 
                     item.ToggleRequested += OnItemToggleRequested;
            }
            if (e.OldItems != null)
            {
                foreach (IExpandableItem item in e.OldItems) 
                     item.ToggleRequested -= OnItemToggleRequested;
            }

            // Recalculate Layout on any collection change
            if (ItemsSource is IEnumerable<IExpandableItem> items)
            {
                 _ = RecalculateLayoutAsync(items);
            }
        }

        private async void OnItemToggleRequested(object sender, EventArgs e)
        {
            if (sender is IExpandableItem item)
            {
                await RunTransactionAsync(item);
            }
        }

        // Transaction Logic: Prevent Glitch
        private async Task RunTransactionAsync(IExpandableItem itemToToggle)
        {
            await _lock.WaitAsync();
            try
            {
                // Safe cast using pattern matching
                if (ItemsSource is not IEnumerable<IExpandableItem> items) return;

                double width = this.ActualWidth;
                
                // Create snapshot for calculation
                var futureStates = items.Select(x => new CardStateSnapshot 
                { 
                    IsExpanded = (x == itemToToggle) ? !x.IsExpanded : x.IsExpanded 
                }).ToList();

                // Pass configuration to engine
                var config = new LayoutConfig(SmallCardSize, LargeCardSize);
                var newSchema = await Task.Run(() => LayoutEngine.Calculate(futureStates, width, config));

                // Commit (UI Thread)
                _layout.SwapSchema(newSchema); 
                itemToToggle.IsExpanded = !itemToToggle.IsExpanded; 
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task RecalculateLayoutAsync(IEnumerable<IExpandableItem> items)
        {
            // Simple recalculate without state toggle
             await RunLayoutUpdateAsync(null, null);
        }

        private async Task RunLayoutUpdateAsync(int? dragIdx, int? targetIdx)
        {
            // Debounce or lock could be used here
            // For now, just run the calculation
             if (ItemsSource is not IEnumerable<IExpandableItem> items) return;
             
             double width = this.ActualWidth;
             if (width == 0) return; // Wait for load

             // Use current state
             var currentStates = items.Select(x => new CardStateSnapshot 
             { 
                 IsExpanded = x.IsExpanded 
             }).ToList();

             var config = new LayoutConfig(SmallCardSize, LargeCardSize);
             var newSchema = await Task.Run(() => LayoutEngine.Calculate(currentStates, width, config, dragIdx, targetIdx));

             _layout.SwapSchema(newSchema);
        }

        // Drag and Drop Logic
        private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
        {
             if (args.Element is UIElement element)
            {
                element.CanDrag = true;
                element.DragStarting += OnItemDragStarting;
            }
        }

        private void OnItemDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            int index = _repeater.GetElementIndex(sender);
            args.Data.Properties.Add("DragSourceIndex", index);
            args.DragUI.SetContentFromDataPackage();
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;

            Point p = e.GetPosition(_repeater);
            if (!e.DataView.Properties.TryGetValue("DragSourceIndex", out object sourceIndexObj)) return;
            int draggingIndex = (int)sourceIndexObj;
            
            int hoverIndex = _layout.GetIndexAtPoint(p);

            if (hoverIndex != -1)
            {
                // Update the layout visual (Shadow Map) via FastCardLayout
                _layout.UpdateDragPosition(draggingIndex, hoverIndex);
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Properties.TryGetValue("DragSourceIndex", out object sourceIndexObj)) return;
            int oldIndex = (int)sourceIndexObj;

            Point p = e.GetPosition(_repeater);
            int newIndex = _layout.GetIndexAtPoint(p);

             // 1. Clear the "Shadow Map" state
            _layout.ClearDragState();

            // 2. Update the REAL truth (ViewModel)
             if (ItemsSource is ObservableCollection<IExpandableItem> col && oldIndex != newIndex && newIndex >= 0 && newIndex < col.Count)
            {
                // Assuming ObservableCollection<T> matches ItemsSource runtime type
                // Use reflection or standard interface if possible. 
                // For this example we assume IExpandableItem collection supports Move or is ObservableCollection
                // Ideally ItemsSource should be ObservableCollection<SomeConcreteType>
                
                // Using dynamic to call Move if it exists
                dynamic collection = ItemsSource;
                try 
                {
                    collection.Move(oldIndex, newIndex);
                }
                catch
                {
                    // Fallback or ignore if not supported
                }
            }
            // If ItemsSource is ObservableCollection, the OnCollectionChanged will trigger RecalculateLayoutAsync
            // which will refresh the view with the new persistent order.
        }

        // Struct for LayoutEngine
        private struct CardStateSnapshot : ICardState
        {
            public bool IsExpanded { get; set; }
        }
    }
}
