namespace rpg;

/// <summary>
/// przesylanie diktow przez jsona jest trudne
/// ta klasa zawiera liste itemow na danych koordynatach
/// </summary>

public class ItemPileDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public List<ItemDto> Items { get; set; } = new();
}
