namespace rpg;

public abstract class Currency : Item
{
    public int Value { get; }

    public Currency(string name, int value, char symbol) : base(name, 0, symbol, 0)
    {
        Value = value;
    }
}
public class Gold : Currency
{
    public Gold(string name, int value, char symbol) : base(name, value, symbol) {}
    public override bool OnPickUp(Player player)
    {
        player.Gold += Value;
        return true;
    }
}
public class Coins : Currency
{
    public Coins(string name, int value, char symbol) : base(name, value, symbol) {}
    public override bool OnPickUp(Player player)
    {
        player.Coins += Value;
        return true;
    }
}
