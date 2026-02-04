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
            if (items.Count == 0) return schema;

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

            // 2. Sequential-Packing Logic
            int columnCount = (int)Math.Max(1, Math.Floor(viewportWidth / config.SmallSize.Width));
            var occupiedSlots = new HashSet<(int col, int row)>();

            // ItemRects will be in VISUAL order (matching mapping)
            int curCol = 0;
            int curRow = 0;

            foreach (int originalIndex in mapping)
            {
                var item = items[originalIndex];
                bool isLarge = item.IsExpanded;

                int itemWidthUnits = isLarge ? 2 : 1;
                int itemHeightUnits = isLarge ? 2 : 1;

                if (itemWidthUnits > columnCount) itemWidthUnits = columnCount;

                bool placed = false;
                while (!placed)
                {
                    // A. Skip occupied slots (e.g. from a 2x2 card in the row above)
                    while (curCol < columnCount && occupiedSlots.Contains((curCol, curRow)))
                    {
                        curCol++;
                    }

                    if (curCol >= columnCount)
                    {
                        curRow++;
                        curCol = 0;
                        continue;
                    }

                    // B. Check if it fits at current Col/Row (Only check Width + Overlap)
                    if (curCol + itemWidthUnits <= columnCount && CanFit(curCol, curRow, itemWidthUnits, itemHeightUnits, occupiedSlots))
                    {
                        // Place it
                        double x = curCol * config.SmallSize.Width;
                        double y = curRow * config.SmallSize.Height;
                        double w = isLarge ? config.LargeSize.Width : config.SmallSize.Width;
                        double h = isLarge ? config.LargeSize.Height : config.SmallSize.Height;

                        schema.ItemRects.Add(new Rect(x, y, w, h));
                        MarkOccupied(curCol, curRow, itemWidthUnits, itemHeightUnits, occupiedSlots);

                        // Advance cursor
                        curCol += itemWidthUnits;
                        if (curCol >= columnCount)
                        {
                            curCol = 0;
                            curRow++;
                        }
                        placed = true;
                    }
                    else
                    {
                        // C. Does not fit in current row. 
                        // Move to next row and try again. 
                        // Important: per user request, we never "go back".
                        curRow++;
                        curCol = 0;
                    }
                }
            }

            // Calculate Metrics
            double maxBottom = 0;
            foreach (var rect in schema.ItemRects)
            {
                if (rect.Bottom > maxBottom) maxBottom = rect.Bottom;
            }
            schema.TotalHeight = maxBottom;
            schema.MaxItemHeight = Math.Max(config.SmallSize.Height, config.LargeSize.Height);

            return schema;
        }

        private static bool CanFit(int col, int row, int w, int h, HashSet<(int, int)> occupied)
        {
            for (int r = row; r < row + h; r++)
            {
                for (int c = col; c < col + w; c++)
                {
                    if (occupied.Contains((c, r))) return false;
                }
            }
            return true;
        }

        private static void MarkOccupied(int col, int row, int w, int h, HashSet<(int, int)> occupied)
        {
            for (int r = row; r < row + h; r++)
            {
                for (int c = col; c < col + w; c++)
                {
                    occupied.Add((c, r));
                }
            }
        }
    }
}
