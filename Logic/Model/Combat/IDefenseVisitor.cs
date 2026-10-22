namespace rpg;

public interface IDefenseVisitor
{
    int Visit(HeavyWeapon w);
    int Visit(LightWeapon w);
    int Visit(MagicWeapon w);
}

public class NormalDefence : IDefenseVisitor
{
    Player _p;
    public NormalDefence(Player p)
    {
        _p = p;
    }

    public int Visit(HeavyWeapon w) => _p.Strength + _p.Luck;
    public int Visit(LightWeapon w) => _p.Dexterity + _p.Luck;
    public int Visit(MagicWeapon w) => _p.Dexterity;
}

public class SneakyDefence : IDefenseVisitor
{
    Player _p;
    public SneakyDefence(Player p)
    {
        _p = p;
    }
    public int Visit(HeavyWeapon w) => _p.Strength;
    public int Visit(LightWeapon w) => _p.Dexterity;
    public int Visit(MagicWeapon w) => 0;
}

public class MagicDefence : IDefenseVisitor
{
    Player _p;
    public MagicDefence(Player p)
    {
        _p = p;
    }
    public int Visit(HeavyWeapon w) => _p.Luck;
    public int Visit(LightWeapon w) => _p.Luck;
    public int Visit(MagicWeapon w) => _p.Mana * 2;
}
