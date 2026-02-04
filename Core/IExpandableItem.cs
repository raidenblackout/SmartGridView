using System;

namespace SmartGrid.Core
{
    public interface IExpandableItem
    {
        bool IsExpanded { get; set; }
        
        // The card says "I want to toggle!" but waits for permission
        event EventHandler<EventArgs> ToggleRequested;
    }
}
