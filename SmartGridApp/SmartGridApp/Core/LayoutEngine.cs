using System;
using System.Collections.Generic;
using Windows.Foundation; // For Rect

namespace SmartGrid.Core
{
    public interface ICardState
    {
        bool IsExpanded { get; }
    }

    public class LayoutSchema
    {
        // The "Map" of where every item should be
        public List<Rect> ItemRects { get; } = new List<Rect>();

        // Maps Visual Slot Index -> Original Data Index
        public int[]? IndexMapping { get; set; }

        public double TotalHeight { get; set; }
        public double MaxItemHeight { get; set; }
    }

    public struct LayoutConfig
    {
        public Size SmallSize { get; }
        public Size LargeSize { get; }

        public LayoutConfig(Size smallSize, Size largeSize)
        {
            SmallSize = smallSize;
            LargeSize = largeSize;
        }
    }

    public static class LayoutEngine
    {
        // Pure function: Input (Data + Width) -> Output (Geometry)
        public static LayoutSchema Calculate(IList<ICardState> items, double viewportWidth, LayoutConfig config, int? dragIdx = null, int? targetIdx = null)
        {
            var schema = new LayoutSchema();

            // 1. Create a "Mapping" list to track original indices in visual order
            var mapping = new List<int>();
            for (int i = 0; i < items.Count; i++) mapping.Add(i);

            if (dragIdx.HasValue && targetIdx.HasValue && dragIdx.Value >= 0 && dragIdx.Value < mapping.Count)
            {
                int movingIdx = mapping[dragIdx.Value];
                mapping.RemoveAt(dragIdx.Value);

                int insertAt = targetIdx.Value;
                if (insertAt > mapping.Count) insertAt = mapping.Count;
                if (insertAt < 0) insertAt = 0;

                mapping.Insert(insertAt, movingIdx);
            }
            schema.IndexMapping = mapping.ToArray();

            double currentX = 0;
            double currentY = 0;
            double currentRowHeight = 0;

            foreach (int originalIndex in mapping)
            {
                var item = items[originalIndex];
                // 2. Determine size based on state (Negative Space: Assume valid data)
                double width = item.IsExpanded ? config.LargeSize.Width : config.SmallSize.Width;
                double height = item.IsExpanded ? config.LargeSize.Height : config.SmallSize.Height;

                // 3. Bin-Packing Logic: Does it fit in the current row?
                if (currentX + width > viewportWidth)
                {
                    // Move to next row
                    currentX = 0;
                    currentY += currentRowHeight;
                    currentRowHeight = 0;
                }

                // 4. Store the coordinate
                schema.ItemRects.Add(new Rect(currentX, currentY, width, height));

                // 5. Advance cursor
                currentX += width;
                currentRowHeight = Math.Max(currentRowHeight, height);
            }

            schema.TotalHeight = currentY + currentRowHeight;
            // Best effort max height calculation
            schema.MaxItemHeight = Math.Max(config.SmallSize.Height, config.LargeSize.Height);
            return schema;
        }
    }
}
