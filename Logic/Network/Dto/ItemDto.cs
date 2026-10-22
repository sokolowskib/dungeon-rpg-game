namespace rpg;

/// <summary>
/// klasa dto dla itemow
/// </summary>

public class ItemDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public char Symbol { get; set; }
    public bool HasSlots { get; set; }
    public List<ItemDto?>? Slots { get; set; }
}
