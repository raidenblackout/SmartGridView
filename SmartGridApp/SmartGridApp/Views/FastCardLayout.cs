using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using SmartGrid.Core;

namespace SmartGrid.Views
{
    public class FastCardLayout : VirtualizingLayout
    {
        private LayoutSchema? _currentSchema;
        private int? _draggingIndex = null;
        private int? _targetIndex = null;
        private Rect? _manualViewport = null;

        public Rect? ManualViewport
        {
            get => _manualViewport;
            set { _manualViewport = value; InvalidateArrange(); }
        }

        public bool HasSchema => _currentSchema != null;

        // Called by ViewModel/SmartGridView when background math is finished
        public void SwapSchema(LayoutSchema newSchema)
        {
            _currentSchema = newSchema;
            // This forces the ItemsRepeater to read the new positions instantly
            InvalidateMeasure();
        }

        // Called during drag to request a "Visual-Only" layout update
        public void UpdateDragPosition(int draggingIndex, int targetIndex)
        {
            if (_draggingIndex == draggingIndex && _targetIndex == targetIndex) return;

            _draggingIndex = draggingIndex;
            _targetIndex = targetIndex;

            // In a real implementation we would request a background calculation here too,
            // but for simplicity we rely on the parent control to trigger the engine 
            // or we could event it out. 
            // Ideally, SmartGridView calls Calculate with these indices and then calls SwapSchema.
            // But this method implies the Layout drives the update. 
            // Actually, based on the chat history (lines 2215), SmartGridView calls this 
            // AND then triggers the update. 
            // Wait, line 2172 says "RequestBackgroundLayoutRefresh();". 
            // We will define an event for that.
            RequestLayoutRefresh?.Invoke(this, ((int)_draggingIndex, (int)_targetIndex));
        }

        public void ClearDragState()
        {
            if (_draggingIndex != null || _targetIndex != null)
            {
                _draggingIndex = null;
                _targetIndex = null;
                // Notify to reset layout
                RequestLayoutRefresh?.Invoke(this, (null, null));
            }
        }

        public event System.EventHandler<(int? dragIdx, int? targetIdx)>? RequestLayoutRefresh;

        public int GetIndexAtPoint(Point p)
        {
            if (_currentSchema == null) return -1;

            // Optimization: Use Binary Search to narrow Y-range
            // We want first item where Top > p.Y - MaxHeight
            double searchThreshold = p.Y - _currentSchema.MaxItemHeight;
            int startIndex = FindFirstVisibleIndex(searchThreshold);

            // Scan locally
            for (int i = startIndex; i < _currentSchema.ItemRects.Count; i++)
            {
                var rect = _currentSchema.ItemRects[i];
                if (rect.Top > p.Y) break; // Passed the point

                if (rect.Contains(p)) return i;
            }

            // If clicked in empty space, assume end of list
            return _currentSchema.ItemRects.Count - 1;
        }

        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            // If no schema, return 0 size
            if (_currentSchema == null) return new Size(availableSize.Width, 0);

            // Return total height calculated in background
            return new Size(availableSize.Width, _currentSchema.TotalHeight);
        }

        protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
        {
            var sw = Stopwatch.StartNew();
            if (_currentSchema == null) return finalSize;

            var visibleRect = context.RealizationRect;
            int itemCount = context.ItemCount;

            // Report metrics based on TRUE visible rect (ManualViewport) or fallback to realization
            Rect trueVisible = _manualViewport ?? visibleRect;

            // "Passive Measurement": We just look up the Rects for the visible range
            // Optimize finding the start index using Binary Search
            // We use the Min of both Tops to ensure we catch everything
            double searchTop = Math.Min(visibleRect.Top, trueVisible.Top);
            int startIndex = FindFirstVisibleIndex(searchTop);

            int realizedCount = 0;
            int visibleRangeStart = -1;
            int visibleRangeEnd = -1;

            int i = startIndex;
            for (; i < itemCount; i++)
            {
                // Safety check
                if (i >= _currentSchema.ItemRects.Count) break;

                var rect = _currentSchema.ItemRects[i];

                // Check termination conditions
                // We must continue as long as we are within EITHER visibleRect OR trueVisible
                bool inRealization = rect.Top <= visibleRect.Bottom;
                bool inTrueVisible = rect.Top <= trueVisible.Bottom;

                if (!inRealization && !inTrueVisible)
                {
                    break;
                }

                // --- ARRANGE LOGIC ---
                // If element is completely above the viewport...
                // (Only skip if it's outside Realization)
                if (rect.Bottom >= visibleRect.Top && rect.Top <= visibleRect.Bottom)
                {
                    int dataIndex = (_currentSchema.IndexMapping != null && i < _currentSchema.IndexMapping.Length)
                                 ? _currentSchema.IndexMapping[i]
                                 : i;

                    var element = context.GetOrCreateElementAt(dataIndex);
                    element.Arrange(rect);

                    // Ghosting Logic
                    if (_draggingIndex.HasValue && dataIndex == _draggingIndex.Value)
                    {
                        element.Opacity = 0.4;
                    }
                    else
                    {
                        element.Opacity = 1.0;
                    }
                }

                // --- METRICS LOGIC ---
                // Item is visible if it intersects trueVisible
                // We use a slightly more optimized check or just the standard intersection
                bool isVisible = rect.Left < trueVisible.Right && rect.Right > trueVisible.Left &&
                                 rect.Top < trueVisible.Bottom && rect.Bottom > trueVisible.Top;

                if (isVisible)
                {
                    if (visibleRangeStart == -1) visibleRangeStart = i;
                    visibleRangeEnd = i;
                    realizedCount++;
                }
            }

            sw.Stop();
            long arrangeTicks = sw.ElapsedTicks;
            long layoutTicks = _currentSchema?.CalculationTimeTicks ?? 0;

            MetricsUpdated?.Invoke(this, new LayoutMetrics(itemCount, realizedCount, visibleRangeStart, visibleRangeEnd, trueVisible, layoutTicks, arrangeTicks));

            return finalSize;
        }

        public event System.EventHandler<LayoutMetrics>? MetricsUpdated;

        public readonly record struct LayoutMetrics(
            int TotalItems,
            int RealizedItems,
            int StartIndex,
            int EndIndex,
            Rect Viewport,
            long LayoutTicks,
            long ArrangeTicks);

        private int FindFirstVisibleIndex(double viewportTop)
        {
            // Binary search for the first item that *might* be visible.
            // Since Top is monotonic, we search for the first item where Top > Viewport.Top - MaxItemHeight.
            // Any item with Top <= Viewport.Top - MaxItemHeight is guaranteed to have Bottom <= Viewport.Top
            // (assuming Height <= MaxItemHeight).

            if (_currentSchema == null) return 0; // Guard

            double buffer = _currentSchema.MaxItemHeight;
            double targetTop = viewportTop - buffer;
            // if (targetTop < 0) targetTop = 0; // Fix: Allow negative target to find items at 0

            var rects = _currentSchema.ItemRects;
            int low = 0;
            int high = rects.Count - 1;
            int result = 0;

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (rects[mid].Top > targetTop)
                {
                    // This item starts after the threshold, it is a candidate for the first visible block?
                    // No, we want the FIRST item that satisfies the condition. 
                    // Wait, if rects[mid].Top > targetTop, it might be visible.
                    // If rects[mid].Top <= targetTop, it is definitely NOT visible (too high up).
                    // So we want the first index where Top > targetTop?
                    // Let's refine:
                    // We want to include everything > targetTop.
                    // And potentially the one just before it? 
                    // Actually, let's look for Top >= targetTop.

                    // Standard LowerBound implementation
                    // If mid is "greater/equal", it's a valid candidate for the start, but maybe there's an earlier one.
                    result = mid; // Potential answer
                    high = mid - 1;
                }
                else
                {
                    // mid is too far up (Top < targetTop)
                    low = mid + 1;
                }
            }
            // If we didn't find any (all < targetTop), start at 0? No, start at Count.
            // If result remains 0, it means either all are > targetTop (start at 0) or logic flow default.

            // Correction: If low > high, 'low' is the insertion point (first element > val).
            // 'result' tracks the last successful 'high=mid-1'.

            // Let's use simple LowerBound pattern:
            return result; // This returns the first index where Rect.Top > targetTop.
        }
    }
}
