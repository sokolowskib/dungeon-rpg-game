namespace rpg;

public class Enemy
{
    public string Name { get; set; }
    public int Health { get; set; }
    public int DMG { get; set; }
    public int Armor { get; set; }
    public char Symbol = 'Ɵ';
    public Combat combat;
    public bool inCombat = false;
    public int TurnCount = 0;
    public Species Species;
    public SoundChannel? Channel { get; set; }
    private Random _rng;
    public int X { get; set; }
    public int Y { get; set; }
    private Map? _map;
    public bool IsHearingNoise = false;
    public bool IsSeeingPlayer = false;
    public (int x, int y)? LastSeenAt = null;
    private Dictionary<(int, int), int>? _gradient;

    public ISoundReaction? OverrideSound;
    public ISightReaction? OverrideSight;
    public int? OverrideSightRange;

    public ISoundReaction EffectiveSound => OverrideSound ?? Species.SoundReaction;
    public ISightReaction EffectiveSight => OverrideSight ?? Species.SightReaction;
    public int EffectiveSightRange => OverrideSightRange ?? Species.SightRange;

    public void SetMap(Map map)
    {
        _map = map;
    }
    public void SetAndSubscribeToChannel(SoundChannel channel)
    {
        Channel = channel;
        channel.Subscribe(this);
    }

    public Enemy(string name, int h, int dmg, int armor, Species species)
    {
        Name = name;
        Health = h;
        DMG = dmg;
        Armor = armor;
        combat = new Combat(this);
        Species = species;
        Species.Subscribe(this);
        _rng = new Random();
    }

    public void ReactToNoise(SoundNotification n, Dictionary<(int, int), int> gradient)
    {
        if (!EffectiveSound.IsActive)
            return;
        if (!gradient.TryGetValue((X, Y), out int level))
            return;

        IsHearingNoise = true;
        _gradient = gradient;
        EventJournalManager.Instance.Log($"{Name} ({X},{Y}) uslyszal halas ze zrodla ({n.source.x},{n.source.y})");
    }

    public void CheckSight(IEnumerable<Player> players, Map map)
    {
        int bestDist = int.MaxValue;
        (int x, int y)? bestPos = null;

        foreach (var player in players)
        {
            int px = player.GetPositionX(), py = player.GetPositionY();
            int dist = Math.Abs(px - X) + Math.Abs(py - Y);
            if (dist > EffectiveSightRange) continue;
            if (!map.HasLineOfSight(X, Y, px, py)) continue;
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = (px, py);
            }
        }

        if (bestPos == null)
        {
            IsSeeingPlayer = false;
            return;
        }
        IsSeeingPlayer = true;
        LastSeenAt = bestPos;
    }

    public (int, int) NextStepBySight(Map map)
    {
        if (LastSeenAt == null) return (X, Y);
        return EffectiveSight.NextStep(X, Y, LastSeenAt.Value.x, LastSeenAt.Value.y, map);
    }

    public (int, int) NextStepBySound()
    {
        if (_gradient == null)
            return (X, Y);

        var next = EffectiveSound.NextStep(X, Y, _gradient);
        if (next == (X, Y))
        {
            IsHearingNoise = false;
            _gradient = null;
        }
        return next;
    }

    public void Move(int x, int y)
    {
        _map!.MoveEnemy(x, y, this);
    }

    public void ReceiveDamage(int dmg)
    {
        Health -= GetActualDamage(dmg);
    }

    int GetActualDamage(int dmg) => Math.Max(0, dmg - Armor);

    public void Die()
    {
        Species.NotifyDeath(this);
        Species.Unsubscribe(this);
        Channel!.Unsubscribe(this);
    }
}
