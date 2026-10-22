namespace rpg;

public class Species
{
    public string Name { get; }

    private readonly IDeathReaction _deathReaction;
    public ISoundReaction SoundReaction { get; }
    public ISightReaction SightReaction { get; }
    public int SightRange { get; }
    public ISoundReaction? AttackedSound { get; }
    public ISightReaction? AttackedSight { get; }
    public int AttackedSightRange { get; }
    private List<Enemy> _subscribers = new List<Enemy>();

    public Species(string name, IDeathReaction reaction, ISoundReaction soundReaction, ISightReaction sightReaction, int sightRange,
                   ISoundReaction? attackedSound = null, ISightReaction? attackedSight = null, int attackedSightRange = 0)
    {
        Name = name;
        _deathReaction = reaction;
        SoundReaction = soundReaction;
        SightReaction = sightReaction;
        SightRange = sightRange;
        AttackedSound = attackedSound;
        AttackedSight = attackedSight;
        AttackedSightRange = attackedSightRange;
    }

    public void ApplyAttackTransformation(Enemy e)
    {
        if (AttackedSound == null || AttackedSight == null) return;
        e.OverrideSound = AttackedSound;
        e.OverrideSight = AttackedSight;
        e.OverrideSightRange = AttackedSightRange;
        EventJournalManager.Instance.Log($"{e.Name} ({e.X},{e.Y}) zmienia zachowanie po ataku gracza");
    }

    public void Subscribe(Enemy e)
    {
        _subscribers.Add(e);
    }
    public void Unsubscribe(Enemy e)
    {
        _subscribers.Remove(e);
    }

    public void NotifyDeath(Enemy dead)
    {
        foreach (var subscriber in _subscribers)
        {
            _deathReaction.Apply(subscriber, dead);
        }
    }
}
