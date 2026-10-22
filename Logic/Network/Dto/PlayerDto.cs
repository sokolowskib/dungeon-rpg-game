namespace rpg;

public class PlayerDto
{
    public int HP { get; set; }
    public int Mana { get; set; }
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Luck { get; set; }
    public int Rage { get; set; }
    public int Gold { get; set; }
    public int Coins { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public string?[] Hands { get; set; } = new string[2];
    public ItemDto?[] Inventory { get; set; } = new ItemDto[5];
    public int SelectedHand { get; set; }
    public int SelectedSlot { get; set; }
    public int ID { get; set; }
    public int Level { get; set; }
    public int WinsTowardsLevel { get; set; }
    public List<string> Messages { get; set; } = new();
    public CombatDto? Combat { get; set; }
    public SlotEditorDto? SlotEditor { get; set; }
}
