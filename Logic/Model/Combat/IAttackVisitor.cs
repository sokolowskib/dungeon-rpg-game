namespace rpg;

public interface IAttackVisitor
{
    int Visit(HeavyWeapon w);
    int Visit(MagicWeapon w);
    int Visit(LightWeapon w);
}

public class NormalAttack : IAttackVisitor
{
    Player _p;
    public NormalAttack(Player p)
    {
        _p = p;
    }

    public int Visit(HeavyWeapon w) => w.Damage;
    public int Visit(LightWeapon w) => w.Damage;
    public int Visit(MagicWeapon w) => 1;
}

public class SneakyAttack : IAttackVisitor
{
    Player _p;
    public SneakyAttack(Player p)
    {
        _p = p;
    }
    public int Visit(HeavyWeapon w) => (int)(w.Damage * 0.5);
    public int Visit(LightWeapon w) => w.Damage * 2;
    public int Visit(MagicWeapon w) => 1;
}

public class MagicAttack : IAttackVisitor
{
    Player _p;
    public MagicAttack(Player p)
    {
        _p = p;
    }

    public int Visit(HeavyWeapon w) => 1;
    public int Visit(LightWeapon w) => 1;
    public int Visit(MagicWeapon w) => w.Damage;
}
