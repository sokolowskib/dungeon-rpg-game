namespace rpg;

public interface IEventJournal
{
    void AddEntry(string entry);
    List<string> GetRecentEntries(int count);
    List<string> GetAllEntries();
    void SaveToFile(string filePath);
}

public class FileEventJournal : IEventJournal
{
    private readonly List<string> _entries = new();

    public void AddEntry(string entry)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _entries.Add($"[{timestamp}] {entry}");
    }

    public List<string> GetRecentEntries(int count)
    {
        int start = Math.Max(0, _entries.Count - count);
        return _entries.GetRange(start, _entries.Count - start);
    }

    public List<string> GetAllEntries() => new List<string>(_entries);

    public void SaveToFile(string filePath)
    {
        File.WriteAllLines(filePath, _entries);
    }
}

public sealed class EventJournalManager
{
    private static EventJournalManager? _instance;
    private static readonly object _lock = new();
    private readonly IEventJournal _journal;

    private EventJournalManager(IEventJournal journal)
    {
        _journal = journal;
    }

    public static void Initialize(IEventJournal journal)
    {
        lock (_lock)
        {
            if (_instance != null)
                throw new InvalidOperationException("Dziennik zdarzen zostal juz zainicjalizowany.");
            _instance = new EventJournalManager(journal);
        }
    }

    public static EventJournalManager Instance
    {
        get {
            lock (_lock)
            {
                if (_instance == null)
                    throw new InvalidOperationException("Dziennik zdarzen nie zostal zainicjalizowany.");
                return _instance;
            }
        }
    }

    public void Log(string entry) => _journal.AddEntry(entry);
    public List<string> GetRecent(int count) => _journal.GetRecentEntries(count);
    public List<string> GetAll() => _journal.GetAllEntries();
    public void Save(string filePath) => _journal.SaveToFile(filePath);
}
