namespace rpg;

public class SlotEditorDto
{
    public int InventoryCursor { get; set; }
    public int? FocusedInventoryIndex { get; set; }
    public int MaxRenderedHeight { get; set; }
    public List<int>? SlotPath { get; set; }
}
