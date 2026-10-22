namespace rpg;
public class Combat
{
    public Enemy _enemy;
    public List<string> FightLog;
    public Combat(Enemy e)
    {
        _enemy = e;
        FightLog = new List<string>();
    }

    public Player? _player;
    public Map? _map;

    public void BeginCombat(Player player, Map map)
    {
        _player = player;
        _map = map;
        FightLog.Add($"Rozpoczęto walkę z {_enemy.Name}!");
    }

    /// <summary>
    /// Metoda symuluje wykonanie tury walki, na podstawie wybranego ataku liczy
    /// defense i dmg i potem odbiera graczowi i wrogowi odpowiednie hp
    /// </summary>
    /// <param name="attackChoice">wybor stylu ataku gracza [1-3]</param>
    /// <returns>odbiera hp i konczy</returns>
    public void ExecuteTurn(int attackChoice)
    {
        if (_player == null)
            throw new InvalidOperationException("Walka nie zostala rozpoczeta");

        IAttackVisitor attack = attackChoice switch {
            1 => new NormalAttack(_player),
            2 => new SneakyAttack(_player),
            3 => new MagicAttack(_player),
            _ => throw new ArgumentOutOfRangeException(nameof(attackChoice))
        };

        IDefenseVisitor defense = attackChoice switch {
            1 => new NormalDefence(_player),
            2 => new SneakyDefence(_player),
            3 => new MagicDefence(_player),
            _ => throw new ArgumentOutOfRangeException(nameof(attackChoice))
        };

        int dmg = 0;
        if (_player.Hands[0] != null && _player.Hands[0] == _player.Hands[1])  // oburacz
        {
            dmg += _player.Hands[0]?.Accept(attack) ?? 0;
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                dmg += _player.Hands[i]?.Accept(attack) ?? 0;
            }
        }

        int realDmg = Math.Max(0, dmg);
        _enemy.ReceiveDamage(realDmg);
        FightLog.Add($"Zadajesz {realDmg} obrażeń!");
        EventJournalManager.Instance.Log($"Gracz atakuje {_enemy.Name} zadajac {realDmg} obrazen");

        if (_enemy.Health <= 0)
        {
            FightLog.Add($"{_enemy.Name} pokonany!");
            EventJournalManager.Instance.Log($"Pokonano przeciwnika: {_enemy.Name}");
            _enemy.Die();
            return;
        }

        int def = 0;

        if (_player.Hands[0] != null && _player.Hands[0] == _player.Hands[1])  // oburacz
        {
            def += _player.Hands[0]?.Accept(defense) ?? 0;
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                def += _player.Hands[i]?.Accept(defense) ?? 0;
            }
        }
        int dmgToPlayer = Math.Max(0, _enemy.DMG - def);
        _player.ReceiveDamage(dmgToPlayer);
        FightLog.Add($"{_enemy.Name} zadaje ci {dmgToPlayer} obrażeń!");
        EventJournalManager.Instance.Log($"{_enemy.Name} atakuje gracza zadajac {dmgToPlayer} obrazen");
    }
}
