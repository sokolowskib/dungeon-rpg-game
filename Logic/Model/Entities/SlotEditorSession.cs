namespace rpg;

public class SlotEditorSession
{
    public int InventoryCursor { get; set; } = 0;
    public int? FocusedInventoryIndex { get; set; } = null;
    public int MaxRenderedHeight { get; set; } = 0;
    public List<int>? SlotPath { get; set; } = null;
}
