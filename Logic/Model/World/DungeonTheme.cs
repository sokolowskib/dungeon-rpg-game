namespace rpg;
public abstract class DungeonTheme : DungeonStrategy
{
    public abstract string IntroMessage { get; }
}

public static class DungeonThemeFactory
{
    private static readonly Dictionary<string, Func<DungeonTheme>> _themes = new() {
        { "minecraft", () => new MinecraftTheme() },
        { "christmas", () => new ChristmasTheme() },
    };

    public static DungeonTheme Create(string name)
    {
        return _themes[name]();
    }
}

public class MinecraftTheme : DungeonTheme
{
    public override string IntroMessage => "W kwadratowej krainie znajduje się dużo nieodkrytych sekretów";

    private Item BuildDebugStick()
    {
        var luck = new StatItemDecorator(new MiscItem("Kamien szczescia", '*'), "+2 LCK", 2, (p, n) => p.Luck += n);
        var str  = new StatItemDecorator(new MiscItem("Kamien sily",      '*'), "+2 STR", 2, (p, n) => p.Strength += n);
        var mna  = new StatItemDecorator(new MiscItem("Kamien madrosci",  '*'), "+2 MNA", 2, (p, n) => p.Mana += n);

        var innerSocket = new Socket("Uchwyt maly", 'u', 2);
        innerSocket.Slots[1] = mna;

        var outerSocket = new Socket("Uchwyt duzy", 'U', 2);
        outerSocket.Slots[0] = str;
        outerSocket.Slots[1] = innerSocket;

        var stick = new SlottedDecorator(new HeavyWeapon(new OneHandedWeapon("Debug Stick", 5, 'Þ')), 2);
        stick.Slots[0] = luck;
        stick.Slots[1] = outerSocket;
        return stick;
    }

    public override void Build(DungeonBuilder builder)
    {
        builder
            .FilledDungeon()
            .AddCorridors(2)
            .AddRooms(1, 3, 4)
            .AddItems(
                new Func<Item>[] {
                    () => new MiscItem("String", '~'),
                    () => new MiscItem("Bones", '-'),
                    () => new MiscItem("Rotten Flesh", '?'),
                    () => new MiscItem("Spider's Eye", '%'),
                    () => new MiscItem("Ender Pearl", 'o'),
                },
                6)
            .AddWeapons(
                new(string name, char symbol, int hands, Func<Item, Item> weaponType)[] {
                    ("Fireworked Crossbow", '|', 1, item => new MagicWeapon(item)),
                    ("Netherite Spear", '/', 1, item => new MagicWeapon(item)),
                    ("Glacier's Drill", 'T', 2, item => new MagicWeapon(item)),
                    ("Diamond Sword", '!', 1, item => new LightWeapon(item)),
                },
                5)
            .PlaceArtifact(new MagicWeapon(new OneHandedWeapon("Mace", 45, '*')))
            .PlaceArtifact(BuildDebugStick())
            .AddSlotItems(6)
            .AddEnemies(new(string name, int hp, int dmg, int armor, Species species, int count)[] {
                ("Warden", 70, 20, 2, new Species("Warden", new BraveReaction(), new AggressiveSound(), new AggressiveSight(), 8), 2),
                ("Ender Dragon", 90, 25, 4, new Species("Ender Dragon", new BraveReaction(), new AggressiveSound(), new AggressiveSight(), 8), 2),
                ("Skeleton", 50, 15, 1, new Species("Skeleton", new WeakReaction(), new CowardlySound(), new CowardlySight(), 6), 3),
                ("Zombie", 40, 18, 0, new Species("Zombie", new WeakReaction(), new CowardlySound(), new CowardlySight(), 6), 3),
                ("Spider", 35, 12, 1, new Species("Spider", new BraveReaction(), new NeutralSound(), new NeutralSight(), 0,
                    new AggressiveSound(), new AggressiveSight(), 8), 3),
                ("Dog", 30, 8, 0, new Species("Dog", new WeakReaction(), new NeutralSound(), new NeutralSight(), 0,
                    new CowardlySound(), new CowardlySight(), 6), 3),
            });
    }
}

public class ChristmasTheme : DungeonTheme
{
    public override string IntroMessage => "Czujesz nadchodzącego ducha świąt";

    public override void Build(DungeonBuilder builder)
    {
        builder.FilledDungeon()
            .AddCorridors()
            .AddRooms(5, 3, 7)
            .AddItems(
                new Func<Item>[] {
                    () => new MiscItem("Advent Calendar", '#'),
                    () => new MiscItem("Icicle", '-'),
                    () => new MiscItem("Candy Cane", '%'),
                    () => new MiscItem("Red Herring", '~'),
                    () => new MiscItem("Dinner Plate", 'o'),
                },
                7)
            .AddWeapons(
                new(string name, char symbol, int hands, Func<Item, Item> weaponType)[] {
                    ("Ski Pole", '|', 1, item => new HeavyWeapon(item)),
                    ("Ice Skate", '+', 1, item => new LightWeapon(item)),
                    ("Snowboard", '!', 1, item => new LightWeapon(item)),
                    ("Freestyle Skis", 'X', 2, item => new HeavyWeapon(item)),
                },
                5)
            .PlaceArtifact(new LightWeapon(new OneHandedWeapon("Snowball Gun", 50, '=')))
            .AddSlotItems(6)
            .AddEnemies(new(string name, int hp, int dmg, int armor, Species species, int count)[] {
                ("Santa", 100, 20, 8, new Species("Snow Creature", new BraveReaction(), new AggressiveSound(), new AggressiveSight(), 8), 2),
                ("Snowman", 60, 12, 5, new Species("Snow Creature", new BraveReaction(), new AggressiveSound(), new AggressiveSight(), 8), 3),
                ("Snowboarder", 40, 22, 2, new Species("Snow Tourist", new WeakReaction(), new CowardlySound(), new CowardlySight(), 6), 2),
                ("Shopping Mall Santa", 120, 28, 10, new Species("Snow Tourist", new WeakReaction(), new CowardlySound(), new CowardlySight(), 6), 2),
            });
    }
}
