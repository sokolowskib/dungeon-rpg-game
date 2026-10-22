using System.Text;
using System.Text.RegularExpressions;

namespace rpg;

public static class Ansi
{
    public const string Reset = "\x1b[0m";
    public const string Red = "\x1b[31m";
    public const string Green = "\x1b[32m";
    public const string Yellow = "\x1b[33m";
    public const string Blue = "\x1b[34m";
    public const string Magenta = "\x1b[35m";
    public const string Cyan = "\x1b[36m";
    public const string White = "\x1b[37m";
    public const string Bold = "\x1b[1m";
    public const string Dim = "\x1b[2m";

    public static string Color(string text, string color) => $"{color}{text}{Reset}";

    public static int VisibleLength(string s) => Regex.Replace(s, @"\x1b\[[0-9;]*m", "").Length;

    public static string PadVisible(string s, int totalWidth)
    {
        int padding = totalWidth - VisibleLength(s);
        return padding > 0 ? s + new string(' ', padding) : s;
    }
}

/// <summary>
/// klasa odpowiedzialna za czesc view z mvc. operuje na gamestate, zeby architektura lokalna i sieciowa korzystala z
/// tych samych metod.
/// </summary>
public class ConsoleView : IGameView
{
    const int BoxWidth = 80;
    const int FooterBoxWidth = 80;
    bool _wasCombat = false;
    bool _wasEditor = false;

    public void Render(GameStateDto state, int myId)
    {
        var me = state.Players.FirstOrDefault(p => p.ID == myId);
        if (me == null)
            return;

        if (me.SlotEditor != null)
            RenderSlotEditor(me);
        else if (me.Combat != null)
            RenderCombat(me);
        else
            RenderWorld(state, me);
    }

    public void ShowIntro(List<string> lines)
    {
        Console.Clear();
        int screenWidth = Console.WindowWidth;
        int screenHeight = Console.WindowHeight;
        var buffer = new List<string>();
        foreach (var line in lines)
        {
            buffer.Add(line);
            Console.Clear();
            int startY = Math.Max(1, screenHeight - buffer.Count - 2);
            for (int i = 0; i < buffer.Count; i++)
            {
                int row = startY + i;
                if (row >= screenHeight - 1)
                    break;
                string text = buffer[i];
                int padLeft = Math.Max(0, (screenWidth - Ansi.VisibleLength(text)) / 2);
                Console.SetCursorPosition(0, row);
                Console.Write(new string(' ', padLeft));
                Console.Write(Ansi.Color(text, Ansi.Yellow + Ansi.Bold));
            }
            Thread.Sleep(350);
        }
        string prompt = "Naciśnij dowolny klawisz...";
        int promptPad = Math.Max(0, (screenWidth - prompt.Length) / 2);
        Console.SetCursorPosition(0, screenHeight - 1);
        Console.Write(new string(' ', promptPad));
        Console.Write(Ansi.Color(prompt, Ansi.Dim));
        Console.ReadKey(intercept: true);
        Console.Clear();
    }

    public void RenderFullJournal(List<string> entries)
    {
        Console.Clear();
        int width = 80;
        string hLine = new string('─', width - 2);
        var sb = new StringBuilder();
        sb.AppendLine($"┌{hLine}┐");
        sb.AppendLine(CombatRow(width, Ansi.Color("DZIENNIK ZDARZEN", Ansi.Green + Ansi.Bold)));
        sb.AppendLine($"├{hLine}┤");
        if (entries.Count == 0)
            sb.AppendLine(CombatRow(width, Ansi.Color("(brak wpisow)", Ansi.Dim)));
        else
            foreach (var e in entries)
                sb.AppendLine(CombatRow(width, e));
        sb.AppendLine($"├{hLine}┤");
        sb.AppendLine(CombatRow(width, Ansi.Color("Nacisnij dowolny klawisz...", Ansi.Dim)));
        sb.AppendLine($"└{hLine}┘");
        Console.Write(sb.ToString());
        Console.ReadKey(intercept: true);
        Console.Clear();
    }

    public void RenderVictory(List<PlayerDto> players)
    {
        Console.Clear();
        int w = Console.WindowWidth;
        int h = Console.WindowHeight;

        var ranked = players.OrderByDescending(p => p.Level).ToList();
        int winnerId = ranked.Count > 0 ? ranked[0].ID : 0;
        int winnerLvl = ranked.Count > 0 ? ranked[0].Level : 0;

        var lines = new List<string> {
            "╔════════════════════════════════╗",
            "║       ZWYCIESTWO!              ║",
            "║   Wszyscy wrogowie pokonani    ║",
            "╚════════════════════════════════╝",
            "",
            $"Zwyciezca: Bohater {winnerId} (LVL {winnerLvl})",
            "",
            "─── KONCOWE WYNIKI ───",
        };
        foreach (var p in ranked)
            lines.Add($"Bohater {p.ID}: LVL {p.Level}  HP {p.HP}  STR {p.Strength}  DEX {p.Dexterity}");

        int startY = Math.Max(0, h / 2 - lines.Count / 2);
        foreach (var line in lines)
        {
            int padLeft = Math.Max(0, (w - line.Length) / 2);
            Console.SetCursorPosition(padLeft, startY++);
            Console.Write(Ansi.Color(line, Ansi.Yellow + Ansi.Bold));
        }
    }

    public void RenderGameOver(string journalFileName = "")
    {
        Console.Clear();
        int w = Console.WindowWidth;
        int h = Console.WindowHeight;
        string[] lines = { "╔═══════════════════════╗", "║      GAME  OVER       ║", "║   Zginąłeś w boju!    ║",
                           "╚═══════════════════════╝" };
        int startY = h / 2 - lines.Length / 2;
        foreach (var line in lines)
        {
            int padLeft = Math.Max(0, (w - line.Length) / 2);
            Console.SetCursorPosition(padLeft, startY++);
            Console.Write(Ansi.Color(line, Ansi.Red + Ansi.Bold));
        }
        if (journalFileName.Length > 0)
        {
            string info = $"Dziennik zapisano: {journalFileName}";
            int infoPad = Math.Max(0, (w - info.Length) / 2);
            Console.SetCursorPosition(infoPad, startY + 1);
            Console.Write(Ansi.Color(info, Ansi.Yellow));
        }
    }

    void RenderWorld(GameStateDto state, PlayerDto me)
    {
        if (_wasCombat || _wasEditor)
        {
            Console.Clear();
            _wasCombat = false;
            _wasEditor = false;
        }
        Console.SetCursorPosition(0, 0);

        var map = state.Map;
        var grid = BuildGrid(state);
        var sidebar = Sidebar(me, state.Journal);
        var footer = Footer(state, me);
        var frame = new StringBuilder();

        for (int y = 0; y <= map.Height; y++)
        {
            for (int x = 0; x <= map.Width; x++)
                frame.Append(grid[y][x]);
            frame.Append("  ");
            if (y < sidebar.Count)
                frame.Append(sidebar[y]);
            frame.AppendLine();
        }
        for (int y = map.Height + 1; y < sidebar.Count; y++)
        {
            frame.Append(new string(' ', map.Width + 1));
            frame.Append("  ");
            frame.Append(sidebar[y]);
            frame.AppendLine();
        }
        frame.Append(footer);
        Console.Write(frame.ToString());
    }

    static string[][] BuildGrid(GameStateDto state)
    {
        var map = state.Map;
        var grid = new string [map.Height + 1][];
        for (int y = 0; y <= map.Height; y++)
        {
            grid[y] = new string[map.Width + 1];
            for (int x = 0; x <= map.Width; x++)
                grid[y][x] = map.Tiles[y][x].ToString();
        }
        foreach (var pile in map.ItemsOnGround)
            if (pile.Items.Count > 0)
                grid[pile.Y][pile.X] = Ansi.Color(pile.Items[0].Symbol.ToString(), Ansi.Yellow);
        foreach (var e in map.Enemies)
            grid[e.Y][e.X] = Ansi.Color(e.Symbol.ToString(), Ansi.Red + Ansi.Bold);
        foreach (var p in state.Players)
            grid[p.Y][p.X] = Ansi.Color(((char)('0' + p.ID)).ToString(), Ansi.Green + Ansi.Bold);
        return grid;
    }

    static List<string> Sidebar(PlayerDto me, List<string> journal)
    {
        var s = new List<string>();
        s.Add(TopBorder("BOHATER " + me.ID));
        s.Add(BoxRow($"LVL:   {Ansi.Color(me.Level.ToString(), Ansi.Cyan + Ansi.Bold)}  {XpBar(me.WinsTowardsLevel, 3, 10)} {me.WinsTowardsLevel}/3"));
        s.Add(BoxRow($"HP:    {Ansi.Color(me.HP.ToString(), Ansi.Red + Ansi.Bold)}"));
        s.Add(BoxRow($"MANA:  {Ansi.Color(me.Mana.ToString(), Ansi.Blue + Ansi.Bold)}"));
        s.Add(BoxRow($"STR:   {Ansi.Color(me.Strength.ToString(), Ansi.White)}"));
        s.Add(BoxRow($"DEX:   {Ansi.Color(me.Dexterity.ToString(), Ansi.White)}"));
        s.Add(BoxRow($"LUCK:  {Ansi.Color(me.Luck.ToString(), Ansi.White)}"));
        s.Add(BoxRow($"RAGE:  {Ansi.Color(me.Rage.ToString(), Ansi.Magenta + Ansi.Bold)}"));
        s.Add(BoxRow(
            $"GOLD:  {Ansi.Color(me.Gold.ToString(), Ansi.Yellow + Ansi.Bold)}  COINS: {Ansi.Color(me.Coins.ToString(), Ansi.Yellow)}"));

        s.Add(MidBorder("EKWIPUNEK"));
        for (int i = 0; i < me.Inventory.Length; i++)
        {
            string ind = (i == me.SelectedSlot ? Ansi.Color(">", Ansi.Cyan + Ansi.Bold) : " ");
            string desc = me.Inventory[i]?.Description ?? Ansi.Color("---", Ansi.Dim);
            s.Add(BoxRow($"{ind}{i + 1} {desc}"));
        }
        s.Add(MidBorder("LAPKI"));
        for (int i = 0; i < 2; i++)
        {
            string ind = (i == me.SelectedHand ? Ansi.Color(">", Ansi.Cyan + Ansi.Bold) : " ");
            string desc = me.Hands[i] ?? Ansi.Color("---", Ansi.Dim);
            s.Add(BoxRow($"{ind}{i + 1} {desc}"));
        }
        s.Add(MidBorder("DZIENNIK"));
        var recent = journal.TakeLast(4).ToList();
        for (int i = 0; i < 4; i++)
        {
            if (i < recent.Count)
                s.Add(BoxRow(Ansi.Color(recent[i], Ansi.Dim)));
            else
                s.Add(BoxRow(""));
        }
        s.Add(BottomBorder());
        return s;
    }

    static string Footer(GameStateDto state, PlayerDto me)
    {
        var sb = new StringBuilder();
        var pile = state.Map.ItemsOnGround.FirstOrDefault(p => p.X == me.X && p.Y == me.Y);
        var items = pile?.Items ?? new List<ItemDto>();

        sb.AppendLine(FooterTopBorder("PODLOGA"));
        if (items.Count > 0)
        {
            for (int i = 0; i < 5; i++)
                sb.AppendLine(FooterRow(i < items.Count ? $"{Ansi.Color("►", Ansi.Yellow)} {items[i].Name}" : ""));
        }
        else
        {
            sb.AppendLine(FooterRow(Ansi.Color("(pusto)", Ansi.Dim)));
            for (int i = 0; i < 4; i++)
                sb.AppendLine(FooterRow(""));
        }

        sb.AppendLine(FooterMidBorder("WIADOMOSCI"));
        for (int i = 0; i < 3; i++)
            sb.AppendLine(FooterRow(i < me.Messages.Count ? $"{Ansi.Color("►", Ansi.Cyan)} {me.Messages[i]}" : ""));

        sb.AppendLine(FooterMidBorder("KONTROLKI"));
        var actions = new List<string>();
        var instr = state.Map.Instructions;
        bool hasEnemy = state.Map.Enemies.Any(e => e.X == me.X && e.Y == me.Y);
        int handItems = me.Hands.Count(h => h != null);
        int invItems = me.Inventory.Count(i => i != null);

        if (me.Messages.Count > 0)
            actions.Add($"{Ansi.Color("P", Ansi.Cyan + Ansi.Bold)} usun wiadomosc");
        if (instr.Contains("drop") && invItems > 0)
            actions.Add($"{Ansi.Color("Q", Ansi.Cyan + Ansi.Bold)} wyrzuc przedmiot");
        if (instr.Contains("equip") && invItems > 0)
            actions.Add($"{Ansi.Color("Tab", Ansi.Cyan + Ansi.Bold)} wez do reki");
        if (instr.Contains("swap_hand") && handItems > 0)
            actions.Add($"{Ansi.Color("G", Ansi.Cyan + Ansi.Bold)} zmiana lapki");
        if (instr.Contains("pickup") && items.Count > 0)
            actions.Add($"{Ansi.Color("E", Ansi.Cyan + Ansi.Bold)} podnies przedmiot");
        if (hasEnemy)
        {
            var enemy = state.Map.Enemies.First(e => e.X == me.X && e.Y == me.Y);
            actions.Add($"{Ansi.Color("F", Ansi.Red + Ansi.Bold)} walcz z {Ansi.Color(enemy.Name, Ansi.Red)}");
        }
        actions.Add($"{Ansi.Color("J", Ansi.Cyan + Ansi.Bold)} dziennik zdarzen");
        actions.Add($"{Ansi.Color("1-5", Ansi.Cyan + Ansi.Bold)} wybor slotu");

        for (int i = 0; i < 6; i++)
            sb.AppendLine(FooterRow(i < actions.Count ? actions[i] : ""));
        sb.AppendLine(FooterBottomBorder());
        return sb.ToString();
    }

    void RenderSlotEditor(PlayerDto me)
    {
        if (!_wasEditor)
        {
            Console.Clear();
            _wasEditor = true;
        }
        Console.SetCursorPosition(0, 0);
        var sb = new StringBuilder();
        int width = 110;
        string hLine = new string('─', width - 2);

        sb.AppendLine($"┌{hLine}┐");
        sb.AppendLine(CombatRow(width, Ansi.Color("⚙  EDYTOR SLOTOW  ⚙", Ansi.Cyan + Ansi.Bold)));
        sb.AppendLine($"├{hLine}┤");
        sb.AppendLine(CombatRow(width, Ansi.Color("EKWIPUNEK", Ansi.Yellow + Ansi.Bold)));
        sb.AppendLine(CombatRow(width, ""));

        var ed = me.SlotEditor!;
        for (int i = 0; i < me.Inventory.Length; i++)
        {
            string marker = (i == ed.InventoryCursor)
                ? Ansi.Color(">", Ansi.Cyan + Ansi.Bold)
                : " ";
            string desc = me.Inventory[i]?.Description ?? Ansi.Color("---", Ansi.Dim);
            sb.AppendLine(CombatRow(width, $"{marker} {i + 1}. {desc}"));
        }

        sb.AppendLine($"├{hLine}┤");
        var detail = new List<string>();
        if (ed.FocusedInventoryIndex != null)
        {
            var focused = me.Inventory[ed.FocusedInventoryIndex.Value];
            string focusedName = focused?.Description ?? "?";
            detail.Add(Ansi.Color($"WYBRANY: {focusedName}", Ansi.Yellow + Ansi.Bold));
            if (focused != null)
                AppendSlotTree(detail, focused.Slots, new List<int>(), ed.SlotPath);
        }
        else
        {
            detail.Add(Ansi.Color("(zadnego przedmiotu nie wybrano)", Ansi.Dim));
        }
        int detailHeight = Math.Max(ed.MaxRenderedHeight + 1, detail.Count);
        for (int i = 0; i < detailHeight; i++)
            sb.AppendLine(CombatRow(width, i < detail.Count ? detail[i] : ""));
        sb.AppendLine($"├{hLine}┤");
        sb.AppendLine(CombatRow(width, Ansi.Color("WIADOMOSCI", Ansi.Yellow + Ansi.Bold)));
        for (int i = 0; i < 3; i++)
            sb.AppendLine(CombatRow(width, i < me.Messages.Count ? $"{Ansi.Color("►", Ansi.Cyan)} {me.Messages[i]}" : ""));
        sb.AppendLine($"├{hLine}┤");
        string pHint = me.Messages.Count > 0
            ? $"   {Ansi.Color("[P]", Ansi.Cyan + Ansi.Bold)} usun wiadomosc"
            : "";
        if (ed.FocusedInventoryIndex != null)
        {
            sb.AppendLine(CombatRow(width, $"{Ansi.Color("[↑↓]", Ansi.Cyan + Ansi.Bold)} slot   " +
                                           $"{Ansi.Color("[Backspace]", Ansi.Cyan + Ansi.Bold)} wyjmij   " +
                                           $"{Ansi.Color("[1-5]", Ansi.Cyan + Ansi.Bold)} wloz z eq   " +
                                           $"{Ansi.Color("[←]", Ansi.Cyan + Ansi.Bold)} cofnij   " +
                                           $"{Ansi.Color("[T]", Ansi.Cyan + Ansi.Bold)} zamknij" + pHint));
        }
        else
        {
            sb.AppendLine(CombatRow(width, $"{Ansi.Color("[↑↓]", Ansi.Cyan + Ansi.Bold)} kursor   " +
                                           $"{Ansi.Color("[Enter]", Ansi.Cyan + Ansi.Bold)} wejdz   " +
                                           $"{Ansi.Color("[T]", Ansi.Cyan + Ansi.Bold)} zamknij" + pHint));
        }
        sb.AppendLine($"└{hLine}┘");

        Console.Write(sb.ToString());
    }

    void AppendSlotTree(List<string> lines, List<ItemDto?>? slots, List<int> prefix, List<int>? cursorPath)
    {
        if (slots == null)
            return;
        string indent = new string(' ', (prefix.Count + 1) * 2);
        for (int i = 0; i < slots.Count; i++)
        {
            var path = new List<int>(prefix) { i };
            bool isCursor = cursorPath != null && path.SequenceEqual(cursorPath);
            string marker = isCursor ? Ansi.Color(">", Ansi.Cyan + Ansi.Bold) : " ";
            var child = slots[i];
            string body = child == null
                ? $"[{i + 1}] {Ansi.Color("(pusty)", Ansi.Dim)}"
                : $"[{i + 1}] {child.Description}";
            lines.Add($"{indent}{marker} {body}");
            if (child != null && child.HasSlots)
                AppendSlotTree(lines, child.Slots, path, cursorPath);
        }
    }

    void RenderCombat(PlayerDto me)
    {
        if (!_wasCombat)
        {
            Console.Clear();
            _wasCombat = true;
        }
        Console.SetCursorPosition(0, 0);
        var sb = new StringBuilder();
        var c = me.Combat!;
        int width = 80;
        string hLine = new string('─', width - 2);

        sb.AppendLine($"┌{hLine}┐");
        sb.AppendLine(CombatRow(width, Ansi.Color("⚔  WALKA  ⚔", Ansi.Red + Ansi.Bold)));
        sb.AppendLine($"├{hLine}┤");

        sb.AppendLine(CombatRow(width, ""));
        sb.AppendLine(CombatRow(width, Ansi.Color(@"   /\_/\   ", Ansi.Red)));
        sb.AppendLine(CombatRow(width, Ansi.Color(@"  ( o.o )  ", Ansi.Red)));
        sb.AppendLine(CombatRow(width, Ansi.Color(@"   > ^ <   ", Ansi.Red)));
        sb.AppendLine(CombatRow(width, ""));

        sb.AppendLine(CombatRow(width, Ansi.Color(c.EnemyName, Ansi.Red + Ansi.Bold)));
        sb.AppendLine(
            CombatRow(width, $"HP: {HpBar(c.EnemyHp, 100, 20)} {Ansi.Color(c.EnemyHp.ToString(), Ansi.Red)}"));
        sb.AppendLine(CombatRow(
            width,
            $"ATK: {Ansi.Color(c.EnemyDamage.ToString(), Ansi.Yellow)}  PANCERZ: {Ansi.Color(c.EnemyArmor.ToString(), Ansi.Blue)}"));

        sb.AppendLine($"├{hLine}┤");
        sb.AppendLine(CombatRow(width, Ansi.Color($"BOHATER {me.ID}", Ansi.Green + Ansi.Bold)));
        sb.AppendLine(CombatRow(width, $"HP: {HpBar(me.HP, 100, 20)} {Ansi.Color(me.HP.ToString(), Ansi.Green)}"));
        sb.AppendLine(CombatRow(width, $"L: {me.Hands[0] ?? Ansi.Color("(puste)", Ansi.Dim)}"));
        sb.AppendLine(CombatRow(width, $"P: {me.Hands[1] ?? Ansi.Color("(puste)", Ansi.Dim)}"));

        sb.AppendLine($"├{hLine}┤");
        int logStart = Math.Max(0, c.FightLog.Count - 5);
        for (int i = logStart; i < c.FightLog.Count; i++)
            sb.AppendLine(CombatRow(width, Ansi.Color("► ", Ansi.Cyan) + c.FightLog[i]));
        for (int i = 0; i < 5 - (c.FightLog.Count - logStart); i++)
            sb.AppendLine(CombatRow(width, ""));

        sb.AppendLine($"├{hLine}┤");
        sb.AppendLine(CombatRow(width, $"{Ansi.Color("[1]", Ansi.Cyan + Ansi.Bold)} Zwykly  " +
                                           $"{Ansi.Color("[2]", Ansi.Cyan + Ansi.Bold)} Skryty  " +
                                           $"{Ansi.Color("[3]", Ansi.Cyan + Ansi.Bold)} Magiczny  " +
                                           $"{Ansi.Color("[Q]", Ansi.Cyan + Ansi.Bold)} Ucieczka"));
        sb.AppendLine($"└{hLine}┘");
        Console.Write(sb.ToString());
    }

    static string HpBar(int current, int max, int barWidth)
    {
        current = Math.Clamp(current, 0, max);
        int filled = max > 0 ? (int)((double)current / max * barWidth) : 0;
        int empty = barWidth - filled;
        return Ansi.Color(new string('█', filled), Ansi.Green) + Ansi.Color(new string('░', empty), Ansi.Dim);
    }

    static string XpBar(int current, int max, int barWidth)
    {
        current = Math.Clamp(current, 0, max);
        int filled = max > 0 ? (int)((double)current / max * barWidth) : 0;
        int empty = barWidth - filled;
        return Ansi.Color(new string('█', filled), Ansi.Cyan) + Ansi.Color(new string('░', empty), Ansi.Dim);
    }

    static string TopBorder(string t) => Border('┌', '┐', BoxWidth, t);
    static string MidBorder(string t) => Border('├', '┤', BoxWidth, t);
    static string BottomBorder() => $"└{new string('─', BoxWidth - 2)}┘";
    static string FooterTopBorder(string t) => Border('┌', '┐', FooterBoxWidth, t);
    static string FooterMidBorder(string t) => Border('├', '┤', FooterBoxWidth, t);
    static string FooterBottomBorder() => $"└{new string('─', FooterBoxWidth - 2)}┘";

    static string Border(char left, char right, int width, string title)
    {
        string coloredTitle = Ansi.Color($" {title} ", Ansi.Green + Ansi.Bold);
        int titleVisible = title.Length + 2;
        int remaining = width - 2 - titleVisible;
        int padLeft = 1;
        int padRight = Math.Max(0, remaining - padLeft);
        return $"{left}{new string('─', padLeft)}{coloredTitle}{new string('─', padRight)}{right}";
    }

    static string BoxRow(string content)
    {
        int innerWidth = BoxWidth - 4;
        return $"│ {Ansi.PadVisible(content, innerWidth)} │";
    }
    static string FooterRow(string content)
    {
        int innerWidth = FooterBoxWidth - 4;
        return $"│ {Ansi.PadVisible(content, innerWidth)} │";
    }
    static string CombatRow(int width, string content)
    {
        int innerWidth = width - 4;
        return $"│ {Ansi.PadVisible(content, innerWidth)} │";
    }
}
