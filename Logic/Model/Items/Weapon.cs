
namespace rpg;

public abstract class Weapon : Item
{
    public Weapon(string name, int damage, char symbol, int handsRequired) : base(name, damage, symbol, handsRequired)
    {
    }
}

public class OneHandedWeapon : Weapon
{
    public OneHandedWeapon(string name, int damage, char symbol) : base(name, damage, symbol, 1) {}

    public override void OnEquipAs(Item self, Player player, Map map)
    {
        player.EquipSingleHand(self, map);
    }
}
public class DoubleHandedWeapon : Weapon
{
    public DoubleHandedWeapon(string name, int damage, char symbol) : base(name, damage, symbol, 2) {}

    public override void OnEquipAs(Item self, Player player, Map map)
    {
        player.EquipDoubleHand(self, map);
    }
}
