namespace rpg;

public interface IDeathReaction
{
    void Apply(Enemy survivor, Enemy dead);
}

class WeakReaction : IDeathReaction
{
    public void Apply(Enemy survivor, Enemy dead)
    {
        survivor.Health -= 5;
        survivor.DMG -= 3;
    }
}

class BraveReaction : IDeathReaction
{
    public void Apply(Enemy survivor, Enemy dead)
    {
        survivor.Armor += 4;
        survivor.DMG += 5;
    }
}
