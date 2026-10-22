namespace rpg;

public abstract class Tile
{
    public abstract char Symbol { get; }
    public abstract bool CanEnter();
}

public class FloorTile : Tile
{
    public override char Symbol => ' ';
    public override bool CanEnter() => true;
}

public class WallTile : Tile
{
    public override char Symbol => '█';
    public override bool CanEnter() => false;
}
